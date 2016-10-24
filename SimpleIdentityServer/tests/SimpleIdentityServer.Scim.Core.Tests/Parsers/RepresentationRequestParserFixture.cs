﻿#region copyright
// Copyright 2015 Habart Thierry
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

using Moq;
using Newtonsoft.Json.Linq;
using SimpleIdentityServer.Scim.Core.Errors;
using SimpleIdentityServer.Scim.Core.Models;
using SimpleIdentityServer.Scim.Core.Parsers;
using SimpleIdentityServer.Scim.Core.Stores;
using System;
using System.Linq;
using Xunit;

namespace SimpleIdentityServer.Scim.Core.Tests.Parsers
{
    public class RepresentationRequestParserFixture
    {
        private Mock<ISchemaStore> _schemaStoreStub;
        private IRepresentationRequestParser _requestParser;

        [Fact]
        public void When_Passing_Null_Parameters_Then_Exceptions_Are_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACTS & ASSERTS
            Assert.Throws<ArgumentNullException>(() => _requestParser.Parse(null, null));
            Assert.Throws<ArgumentNullException>(() => _requestParser.Parse(new JObject(), null));
        }

        [Fact]
        public void When_Schema_Doesnt_Exist_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();
            _schemaStoreStub.Setup(s => s.GetSchema(It.IsAny<string>()))
                .Returns((SchemaResponse)null);

            // ACT & ASSERT
            var exception = Assert.Throws<InvalidOperationException>(() => _requestParser.Parse(new JObject(), "invalid"));
        }

        [Fact]
        public void When_Expecting_Array_But_Its_Singular_Then_Exception_Is_Thrown()
        {
            var schemaStore = new SchemaStore();
            // ARRANGE
            InitializeFakeObjects();
            _schemaStoreStub.Setup(s => s.GetSchema(It.IsAny<string>()))
                .Returns(schemaStore.GetSchema(Constants.SchemaUrns.Group));
            var jObj = JObject.Parse(@"{'schemas': ['urn:ietf:params:scim:schemas:core:2.0:Group']," +
            "'displayName': 'Group A'," +
            "'members': 'members'" +
           "}");

            // ACT & ASSERT
            var exception = Assert.Throws<InvalidOperationException>(() => _requestParser.Parse(jObj, Constants.SchemaUrns.Group));
            Assert.True(exception.Message == string.Format(ErrorMessages.TheAttributeIsNotAnArray, "members"));
        }
        
        [Fact]
        public void When_Expecting_Boolean_But_Its_A_String_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            var schemaStore = new SchemaStore();
            InitializeFakeObjects();
            _schemaStoreStub.Setup(s => s.GetSchema(It.IsAny<string>()))
                .Returns(schemaStore.GetSchema(Constants.SchemaUrns.User));
            var jObj = JObject.Parse(@"{'schemas': ['urn:ietf:params:scim:schemas:core:2.0:User']," +
            "'externalId': 'bjensen'," +
            "'userName': 'bjensen'," +
            "'name': {" +
                "'familyName': 'Jensen'," +
                "'givenName':'Barbara'" +
            "},"+
            "'active': 'active'}");

            // ACT & ASSERT
            var exception = Assert.Throws<InvalidOperationException>(() => _requestParser.Parse(jObj, Constants.SchemaUrns.User));
            Assert.True(exception.Message == string.Format(ErrorMessages.TheAttributeTypeIsNotCorrect, "active", Constants.SchemaAttributeTypes.Boolean));
        }

        [Fact]
        public void When_Simple_PostGroupRequest_Is_Parsed_Then_CorrectValues_Are_Returned()
        {
            var schemaStore = new SchemaStore();
            // ARRANGE
            InitializeFakeObjects();
            _schemaStoreStub.Setup(s => s.GetSchema(It.IsAny<string>()))
                .Returns(schemaStore.GetSchema(Constants.SchemaUrns.Group));
            var jObj = JObject.Parse(@"{'schemas': ['urn:ietf:params:scim:schemas:core:2.0:Group'],"+
            "'displayName': 'Group A',"+
            "'members': ["+
             "{"+
               "'type': 'Group',"+
               "'value': 'bulkId:ytrewq'"+
             "}"+
           "]}");

            // ACT
            var result = _requestParser.Parse(jObj, Constants.SchemaUrns.Group);

            // ASSERTS
            Assert.NotNull(result);
            Assert.True(result.Attributes.Count() == 2);
            Assert.True(result.Attributes.First().SchemaAttribute.Name == "displayName");
            var members = result.Attributes.ElementAt(1) as ComplexRepresentationAttribute;
            Assert.NotNull(members);
            Assert.True(members.SchemaAttribute.Name == "members");
            Assert.True(members.Values.Count() == 1);
            var value = members.Values.First() as ComplexRepresentationAttribute;
            foreach (var subValue in value.Values)
            {
                var singularAttribute = subValue as SingularRepresentationAttribute<string>;
                Assert.True(new[] { "type", "value" }.Contains(singularAttribute.SchemaAttribute.Name));
                Assert.True(new[] { "Group", "bulkId:ytrewq" }.Contains(singularAttribute.Value));
            } 
        }

        [Fact]
        public void When_Complex_PostGroupRequest_Is_Parsed_Then_CorrectValues_Are_Returned()
        {
            var schemaStore = new SchemaStore();
            // ARRANGE
            InitializeFakeObjects();
            _schemaStoreStub.Setup(s => s.GetSchema(It.IsAny<string>()))
                .Returns(schemaStore.GetSchema(Constants.SchemaUrns.Group));
            var jObj = JObject.Parse(@"{'schemas': ['urn:ietf:params:scim:schemas:core:2.0:Group']," +
            "'displayName': 'Group A'," +
            "'members': [" +
             "{" +
               "'type': 'Group'," +
               "'value': 'bulkId:ytrewq'" +
             "}," +
             "{" +
               "'type': 'Group'," +
               "'value': 'bulkId:ytrewq'" +
             "}," +
             "{" +
               "'type': 'Group'," +
               "'value': 'bulkId:ytrewq'" +
             "}" +
           "]}");

            // ACT
            var result = _requestParser.Parse(jObj, Constants.SchemaUrns.Group);


            // ASSERTS
            Assert.NotNull(result);
            Assert.True(result.Attributes.Count() == 2);
            Assert.True(result.Attributes.First().SchemaAttribute.Name == "displayName");
            var members = result.Attributes.ElementAt(1) as ComplexRepresentationAttribute;
            Assert.NotNull(members);
            Assert.True(members.SchemaAttribute.Name == "members");
            Assert.True(members.Values.Count() == 3);
            var value = members.Values.First() as ComplexRepresentationAttribute;
            foreach (var subValue in value.Values)
            {
                var singularAttribute = subValue as SingularRepresentationAttribute<string>;
                Assert.True(new[] { "type", "value" }.Contains(singularAttribute.SchemaAttribute.Name));
                Assert.True(new[] { "Group", "bulkId:ytrewq" }.Contains(singularAttribute.Value));
            }
        }

        [Fact]
        public void When_Simple_PostUserRequest_Is_Parsed_Then_CorrectValues_Are_Returned()
        {
            var schemaStore = new SchemaStore();
            // ARRANGE
            InitializeFakeObjects();
            _schemaStoreStub.Setup(s => s.GetSchema(It.IsAny<string>()))
                .Returns(schemaStore.GetSchema(Constants.SchemaUrns.User));
            var jObj = JObject.Parse(@"{'schemas': ['urn:ietf:params:scim:schemas:core:2.0:User']," +
            "'externalId': 'bjensen'," +
            "'userName': 'bjensen'," +
            "'name': {" +
                "'familyName': 'Jensen',"+
                "'givenName':'Barbara'" +
            "}}");

            // ACT
            var result = _requestParser.Parse(jObj, Constants.SchemaUrns.Group);


            // ASSERTS
            Assert.NotNull(result);
            Assert.True(result.Attributes.Count() == 3);
        }

        private void InitializeFakeObjects()
        {
            _schemaStoreStub = new Mock<ISchemaStore>();
            _requestParser = new RepresentationRequestParser(_schemaStoreStub.Object);
        }
    }
}