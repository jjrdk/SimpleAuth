﻿#region copyright
// Copyright 2017 Habart Thierry
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleIdentityServer.EventStore.EF.Parsers
{
    public class InnerJoinInstruction : BaseInstruction
    {
        private const string innerKey = "inner";
        private const string outerKey = "outer";
        private const string selectKey = "select";
        private readonly IEnumerable<string> parameters = new List<string>
        {
            innerKey,
            outerKey
        };

        private readonly IEnumerable<string> selectParameters = new List<string>
        {
            innerKey,
            outerKey
        };

        public const string Name = "join";

        public InnerJoinInstruction()
        {
        }

        public override KeyValuePair<string, Expression>? GetExpression<TSource>(Type sourceType, ParameterExpression rootParameter, IEnumerable<TSource> source)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            // 1. Split the value & extract the field names or requests.
            var splitted = Parameter.Split(',');
            var instructions = splitted.Select(s =>
            {
                return InstructionHelper.ExtractInstruction(s);
            }).Where(s => s.HasValue);
            if (!parameters.Any(p => instructions.Any(i => i.Value.Key == p)))
            {
                throw new ArgumentException("either inner or outer parameter is not specified");
            }

            // 2. Construct the expression.
            var outer = instructions.First(i => i.Value.Key == outerKey).Value.Value;
            var inner = instructions.First(i => i.Value.Key == innerKey).Value.Value;
            var select = instructions.FirstOrDefault(i => i.Value.Key == selectKey);
            var selectValue = string.Empty;
            if (select != null)
            {
                selectValue = select.Value.Value;
            }

            var tResult = sourceType;
            var innerType = sourceType;
            var propertyInfoOuter = sourceType.GetProperty(outer);
            var propertyInfoInner = sourceType.GetProperty(inner);
            var outerArg = Expression.Parameter(sourceType, "x");
            var innerArg = Expression.Parameter(sourceType, "y");
            var propertyOuter = Expression.Property(outerArg, outer);
            var propertyInner = Expression.Property(innerArg, inner);
            var selectorOuter = Expression.Lambda(propertyOuter, new ParameterExpression[] { outerArg });
            var selectorInner = Expression.Lambda(propertyInner, new ParameterExpression[] { innerArg });
            LambdaExpression selectorResult = null;
            if (string.IsNullOrWhiteSpace(selectValue))
            {
                selectorResult = Expression.Lambda(outerArg, new ParameterExpression[] { outerArg, innerArg });
            }
            else
            {
                var selectAttributes = selectValue.Split('|').Select(v =>
                {
                    var values = v.Split('$');
                    return new KeyValuePair<string, IEnumerable<string>>(values.First(), values.Count() != 2 ? new List<string>() : new List<string>() { values.ElementAt(1) });
                });

                if (!selectAttributes.Any(a => selectParameters.Contains(a.Key)))
                {
                    throw new InvalidOperationException("At least one parameter in select is not supported");
                }

                Dictionary<string, Type> mapping = new Dictionary<string, Type>();
                var outerSelect = selectAttributes.FirstOrDefault(a => a.Key == outerKey);
                var innerSelect = selectAttributes.FirstOrDefault(a => a.Key == innerKey);
                AddTypes(mapping, "outer", sourceType, outerSelect);
                AddTypes(mapping, "inner", sourceType, innerSelect);
                var anonymousType = ReflectionHelper.CreateNewAnonymousType(mapping);
                var parameters = new List<Expression>();
                var tmp = GetParameterExpressions(outerArg, outerSelect);
                if (tmp != null)
                {
                    parameters.AddRange(tmp);
                }

                tmp = GetParameterExpressions(innerArg, innerSelect);
                if (tmp != null)
                {
                    parameters.AddRange(tmp);
                }

                var newExpr = Expression.New(anonymousType.DeclaredConstructors.First(), parameters);
                selectorResult = Expression.Lambda(newExpr, new ParameterExpression[] { innerArg, outerArg });
                tResult = anonymousType.AsType();
            }

            var enumarableType = typeof(Queryable);
            var method = enumarableType.GetMethods().Where(m => m.Name == "Join" && m.IsGenericMethodDefinition).Where(m => m.GetParameters().ToList().Count == 5).First();
            var genericMethod = method.MakeGenericMethod(sourceType, innerType, propertyInfoOuter.PropertyType, tResult);
            var call = Expression.Call(genericMethod, Expression.Constant(source), Expression.Constant(source), selectorOuter, selectorInner, selectorResult);
            return new KeyValuePair<string, Expression>(Name, call);
        }

        private static void AddTypes(Dictionary<string, Type> dic, string name, Type type, KeyValuePair<string, IEnumerable<string>> select)
        {
            if (IsEmpty(select))
            {
                return;
            }

            var propertyTypes = select.Value.Select(v =>
            {
                var prop = type.GetProperty(v);
                return new
                {
                    Name = name + "_" + prop.Name,
                    Type = prop.PropertyType
                };
            });

            if (!propertyTypes.Any())
            {
                dic.Add(name, type);
            }
            else
            {
                foreach (var propertyType in propertyTypes)
                {
                    dic.Add(propertyType.Name, propertyType.Type);
                }
            }
        }

        private static IEnumerable<Expression> GetParameterExpressions(ParameterExpression arg, KeyValuePair<string, IEnumerable<string>> parameter)
        {
            if (IsEmpty(parameter))
            {
                return null;
            }

            if (parameter.Value == null || !parameter.Value.Any())
            {
                return new Expression[] { arg };
            }

            var result = new List<Expression>();
            foreach(var parameterName in parameter.Value)
            {
                result.Add(Expression.Property(arg, parameterName));
            }

            return result;
        }

        private static bool IsEmpty<T, S>(KeyValuePair<T, S> kvp)
        {
            return kvp.Equals(default(KeyValuePair<T, S>));
        }
    }
}