
using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Lxdn.Core.Aggregates;
using Lxdn.Core.Basics;
using Lxdn.Core.Dynamics;
using Lxdn.Core.Extensions;
using Lxdn.Core.Injection;

using Lxdn.Core._MSTests.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lxdn.Core._MSTests
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void TestCastingNonEmptyStringRepresentingInt32ToNullable()
        {
            var nullable = "5".To<int?>();
            Assert.AreEqual(nullable, 5);
        }

        [TestMethod]
        public void TestCastingEmptyStringToNullable()
        {
            var nullable = "".To<int?>();
            Assert.AreEqual(nullable, null);
        }

        [TestMethod]
        public void TestCastingEnumWithFlags()
        {
            var colors = "Red, Green".To<Colors>();
            Assert.AreEqual(Colors.Red | Colors.Green, colors);
        }

        [TestMethod]
        public void TestCaseInsensitiveExpando()
        {
            var expando = new CaseInsensitiveExpando();
            expando.Set("OBJECTID", "12345");
            dynamic d = expando;
            var objectId = d.ObjectId;
            var vehicle = new TestVehicle(d.ObjectId);
            Assert.IsTrue(vehicle.Id == "12345");

            expando.Set("State", 1);
            var state = (Colors)d.State;
        }

        [TestMethod]
        public void TestChangeTypeFromStringToEnum()
        {
            var color = "Red".ChangeType<Colors>();
            Assert.IsTrue(Colors.Red == color);
        }

        [TestMethod]
        public void TestChangeTypeFromIntToBoolean()
        {
            var b = 1.ChangeType<bool>();
            Assert.IsTrue(b);

            b = 0.ChangeType<bool>();
            Assert.IsTrue(!b);
        }

        [TestMethod]
        public void TestToDateTimeOffset()
        {
            var dt = DateTime.Now;
            var off = dt.ToOffset();

            var min = DateTime.MinValue;
        }

        [TestMethod]
        public void TestAnonimizationOfBidderId()
        {
            var a = "Alex".Anonymize("Lxdn");
            //Assert.IsTrue("70E354A476993CEB683AF63C1C0D44DD".Equals(a));

        }

        [TestMethod]
        public void TestChangeTypeFromIntToEnum()
        {
            var color = 1.ChangeType<Colors>();
            Assert.IsTrue(Colors.Red == color);
        }

        [TestMethod]
        public void TestConvertTrueStringToBoolean()
        {
            var b = "true".ChangeType<bool>();
            Assert.IsTrue(b);

            b = "FALSE".ChangeType<bool>();
            Assert.IsTrue(!b);
        }

        [TestMethod]
        public void TestChangeTypeFromIntStringToBoolean()
        {
            var b = "1".ChangeType<bool>();
            Assert.IsTrue(b);

            b = "0".ChangeType<bool>();
            Assert.IsTrue(!b);
        }

        [TestMethod]
        public void TestStripNonsignificantZeros()
        {
            var d1 = 100.0M.StripNonsignificantZeros();
            Assert.AreEqual("100", d1.ToString(CultureInfo.InvariantCulture));

            var d2 = 100M.StripNonsignificantZeros();
            Assert.AreEqual("100", d2.ToString(CultureInfo.InvariantCulture));

            var d3 = 100.1M.StripNonsignificantZeros();
            Assert.AreEqual("100.1", d3.ToString(CultureInfo.InvariantCulture));

            var d4 = 100.01M.StripNonsignificantZeros();
            Assert.AreEqual("100.01", d4.ToString(CultureInfo.InvariantCulture));
        }

        [TestMethod]
        public async Task TestAsyncLock()
        {
            using (var asyncLock = new AsyncLock())
            using (await asyncLock.LockAsync())
            {
                await Task.Delay(1000);
            }
        }

        //[TestMethod]
        public async Task TestAsyncLock_Reenterability()
        {
            // hangs! consider using http://www.hanselman.com/blog/ComparingTwoTechniquesInNETAsynchronousCoordinationPrimitives.aspx

            using (var asyncLock = new AsyncLock())
            using (await asyncLock.LockAsync())
            using (await asyncLock.LockAsync())
            {
                await Task.Delay(1000);
            }
        }

        [TestMethod]
        public void TestIsOneOf()
        {
            var result = Colors.Red.IsOneOf(Colors.Blue, Colors.Green);
        }
        
        [TestMethod]
        public void TestNullableEnum()
        {
            bool? b = true;
            var c = b.IfExists<bool?, TestEnum?>(b1 => b1.Value.ChangeType<TestEnum>());

            Assert.AreEqual(c, TestEnum.Value1);
        }

        [TestMethod]
        public void Test_GetBy()
        {
            var birthday = DateTime.Parse("1976-09-17", CultureInfo.InvariantCulture);
            var persons = new[] {
                new Person("Alex", birthday),
                new Person("Nicolas", DateTime.Now), new Person("Axel", DateTime.Now),
            };

            var gotBy = persons.GetBy(new { Name = "Alex" });
            Assert.AreEqual(1, gotBy.Count());
            var alex = gotBy.Single();
            Assert.AreEqual(alex.Name, "Alex");

            var gotBy2 = persons.GetBy(new { Name = "Alex", Birthday = birthday });
            Assert.AreEqual(1, gotBy.Count());
            alex = gotBy.Single();
            Assert.AreEqual(alex.Name, "Alex");

        }

        [TestMethod]
        public void TestExpando_SetPropertyAsIndex()
        {
            var property = "prop1";
            dynamic expando = new CaseInsensitiveExpando();
            expando.Set(property, 42);
            Assert.AreEqual(42, expando[property]);
            expando[property] = 43;
            Assert.AreEqual(43, expando[property]);
        }

        [TestMethod]
        public void Test_ToDynamic_IgnoresDefaultValues()
        {
            var person = new Person { Name = "Alex" };
            person.Relatives = new List<Person>();
            person.Relatives.Add(new Person { Name = "Sofia" });
            var d = person.ToDynamic();
            // todo: complete the test
            var back = ((object)d).InjectTo<Person>();
        }

        [TestMethod]
        public void Test_PropertyOf_Success()
        {
            var person = new Person { Name = "Alex" };
            Assert.AreEqual("Alex", person.PropertyOf("Name").Value);
        }

        [TestMethod]
        //[ExpectedException(typeof(ArgumentException))]
        public void Test_PropertyOf_ThrowsForNonExistingProperty()
        {
            var person = new Person { Name = "Alex" };
            var nonexisting = person.PropertyOf("Name1");
            Assert.IsNull(nonexisting);
        }

#if NETFULL

        [TestMethod]
        public void Test_Validation2()
        {
            var person = new Person { Name = "Alex" };
            person.Relatives = new List<Person>();
            person.Relatives.Add(new Person { Name = "Sofia" });

            var validator = new AnnotationValidator();
            var results = validator.Validate(person).ToList();

            Assert.AreEqual(5, results.Count);
            Assert.IsTrue(!results.SingleOrDefault(result =>
                result.Attribute.GetType() == typeof(RequiredAttribute) && result.Path.ToString() == "Person.NullArg").HasDefaultValue());
            Assert.IsTrue(!results.SingleOrDefault(result =>
                result.Attribute.GetType() == typeof(RequiredAttribute) && result.Path.ToString() == "Person.Relatives[0].NullArg").HasDefaultValue());
            Assert.IsTrue(!results.SingleOrDefault(result =>
                result.Attribute.GetType() == typeof(RequiredAttribute) && result.Path.ToString() == "Person.Relatives[0].Relatives").HasDefaultValue());

            Assert.AreEqual(2, results.Select(result => result.Attribute).OfType<CustomValidationAttribute>().Count(custom => custom.ValidatorType == typeof(PersonValidator)));
        }
#endif

        [TestMethod]
        public void Test_Combinations_OfStrings()
        {
            var towns = new[] { "Solingen", "Neuss", "D�sseldorf" };
            var combinations = towns.Combinations();

            Assert.AreEqual(7, combinations.Count());
            Assert.AreEqual(3, combinations.Count(c => c.Count() == 1));
            Assert.AreEqual(3, combinations.Count(c => c.Count() == 2));
            Assert.AreEqual(1, combinations.Count(c => c.Count() == 3));
        }

        [TestMethod]
        public void Test_Combinations_OfPersons()
        {
            var persons = new[] { new Person { Name = "Alex" }, new Person { Name = "Christian" }, new Person { Name = "Thorsten" } };
            var combinations = persons.Combinations();

            Assert.AreEqual(7, combinations.Count());
            Assert.AreEqual(3, combinations.Count(c => c.Count() == 1));
            Assert.AreEqual(3, combinations.Count(c => c.Count() == 2));
            Assert.AreEqual(1, combinations.Count(c => c.Count() == 3));
        }

        [TestMethod]
        public void Test_Sequence()
        {
            var sequence = new Sequence<double>(0, 1, 0.2);
            var values = sequence.Yield().ToList();
            Assert.AreEqual(6, values.Count);
        }

        [TestMethod]
        public void Test_Sequence_Backwards()
        {
            var sequence = new Sequence<double>(1, 0, -0.2);
            var values = sequence.Yield().ToList();
            Assert.AreEqual(6, values.Count);
        }

        [TestMethod]
        public void Test_InjectCollectionOfStrings()
        {
            var result = (new { EquipmentItems = new List<string> { "LED", "NAV" } }).InjectTo<Vehicle>();
            Assert.AreEqual(2, result.EquipmentItems.Count);
        }

        //[TestMethod]
        //public void Test_JsonConverter()
        //{
        //    var person = new Person("Alex", new DateTime(1976, 9, 17));
        //    var settings = new JsonSerializerSettings { ContractResolver = new MyResolver() };
        //    var json = JsonConvert.SerializeObject(person, settings);
        //}

        [TestMethod]
        public async Task Test_IfNotNull_OnTask_ReturnsTaskOfNull()
        {
            Func<string, Task<string>> test = s => Task.FromResult<string>(null);

            string nullString = null;

            var result = await nullString.IfExists(test);
            Assert.AreEqual(null, result);

        }

        [TestMethod]
        public void Test_ConvertEnum_ToNullableInt()
        {
            var status = MaritalStatus.Married;
            var value = status.ChangeType<int?>();
            Assert.AreEqual(value, 3);
            var status1 = value.ChangeType<MaritalStatus>();
            Assert.AreEqual(status1, MaritalStatus.Married);
        }

        [TestMethod]
        public void Test_CastToNullable()
        {
            Assert.AreEqual(10, "10".To<int?>());
            Assert.AreEqual(null, "".To<int?>());
            Assert.AreEqual(null, ((string)null).To<int?>());
        }

        [TestMethod]
        public void Test_ChangeTypeNullableToValueType()
        {
            double? d = 5;
            var i = d.ChangeType<int>();
            Assert.AreEqual(5, i);
            d = null;
            i = d.ChangeType<int>();
            Assert.AreEqual(default(int), i);
        }

        [TestMethod]
        public void Test_NewAccessor()
        {
            var person = new Person { Name = "Alex" };
            var property = person.SafelyBrowse(p => p.Name);
            var name = property.GetValue();
            Assert.AreEqual("Alex", name);

            var maritalStatus = person.SafelyBrowse(p => p.MaritalStatus);

            property.SetValue("Tony");
            Assert.AreEqual("Tony", property.GetValue());
        }

        [TestMethod]
        public void Test_InjectStringArrayIntoStringArray()
        {
            var message = new Message { AdditionalInfo = "Foo".Once().ToArray() };
            var injected = message.InjectTo<Message>();

            Assert.AreEqual(true, injected.AdditionalInfo != null);
            Assert.AreEqual(1, injected.AdditionalInfo.Length);
            Assert.AreEqual("Foo", injected.AdditionalInfo[0]);
        }

        [TestMethod]
        public void Test_EncryptDecrypt()
        {
            var key = "dolnik";
            var encrypted = "alex".Encrypt(key);
            var decrypted = encrypted.Decrypt(key);
        }

        [TestMethod]
        public void Test_SymmetricEncryptDecrypt()
        {
            var key = "VALUEpilot2-2018";
            var vector = "VALUEpilot2-2018";

            var data = "Hello world".SymmetricEncrypt(key, vector);

            var x = data.SymmetricDecrypt(key, vector);
            Assert.AreEqual("Hello world", x);
        }

        [TestMethod]
        public void Test_LinqXor()
        {
            var first = new[] { new TestEntity { Id = "LED" }, new TestEntity { Id = "KLA" }, new TestEntity { Id = "KLM" } };
            var second = new[] { new TestEntity { Id = "ALU" }, new TestEntity { Id = "KLA" }, new TestEntity { Id = "KLM" } };

            var xored = first.Xor(second).ToList();
            Assert.AreEqual(2, xored.Count);
            Assert.IsTrue(xored.Any(x => x.Id == "LED"));
            Assert.IsTrue(xored.Any(x => x.Id == "ALU"));
        }
    }
}