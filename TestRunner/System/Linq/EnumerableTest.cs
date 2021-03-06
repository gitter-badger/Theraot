﻿// ReSharper disable PossibleInvalidOperationException

using System;
using System.Linq;

namespace TestRunner.System.Linq
{
    [TestFixture]
    public static class EnumerableTest
    {

        [Test]
        public static void MaxOfEmptyEnumerableThrows()
        {
            Assert.Throws<InvalidOperationException, double>(() => new double[] { }.Max());
            Assert.Throws<InvalidOperationException, float>(() => new float[] { }.Max());
        }
        [Test]
        public static void MaxOfEmptyNullableEnumerableIsNull()
        {
            Assert.AreEqual(null, new double?[] { }.Max());
            Assert.AreEqual(null, new float?[] { }.Max());
        }

        [Test]
        public static void MaxOfNaNisNaN()
        {
            Assert.IsTrue(double.IsNaN(new[] { double.NaN }.Max()));
            Assert.IsTrue(float.IsNaN(new[] { float.NaN }.Max()));
        }

        [Test]
        public static void MaxOfNaNMinValueIsMinValue()
        {
            Assert.AreEqual(double.MinValue, new[] { double.NaN, double.MinValue }.Max());
            Assert.AreEqual(float.MinValue, new[] { float.NaN, float.MinValue }.Max());
            Assert.AreEqual(double.MinValue, new[] { double.MinValue, double.NaN }.Max());
            Assert.AreEqual(float.MinValue, new[] { float.MinValue, float.NaN }.Max());
        }

        [Test]
        public static void MaxOfNaNMinValueWithNullsIsMinValue()
        {
            Assert.AreEqual(double.MinValue, new double?[] { null, double.NaN, double.MinValue }.Max());
            Assert.AreEqual(float.MinValue, new float?[] { null, float.NaN, float.MinValue }.Max());
            Assert.AreEqual(double.MinValue, new double?[] { null, double.MinValue, double.NaN }.Max());
            Assert.AreEqual(float.MinValue, new float?[] { null, float.MinValue, float.NaN }.Max());

            Assert.AreEqual(double.MinValue, new double?[] { double.NaN, null, double.MinValue }.Max());
            Assert.AreEqual(float.MinValue, new float?[] { float.NaN, null, float.MinValue }.Max());
            Assert.AreEqual(double.MinValue, new double?[] { double.MinValue, null, double.NaN }.Max());
            Assert.AreEqual(float.MinValue, new float?[] { float.MinValue, null, float.NaN }.Max());

            Assert.AreEqual(double.MinValue, new double?[] { double.NaN, double.MinValue, null }.Max());
            Assert.AreEqual(float.MinValue, new float?[] { float.NaN, float.MinValue, null }.Max());
            Assert.AreEqual(double.MinValue, new double?[] { double.MinValue, double.NaN, null }.Max());
            Assert.AreEqual(float.MinValue, new float?[] { float.MinValue, float.NaN, null }.Max());
        }

        [Test]
        public static void MaxOfNullableNaNAndNullIsNaN()
        {
            Assert.IsTrue(double.IsNaN(new double?[] { double.NaN, null }.Max().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { float.NaN, null }.Max().Value));
            Assert.IsTrue(double.IsNaN(new double?[] { null, double.NaN }.Max().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { null, float.NaN }.Max().Value));
        }

        [Test]
        public static void MaxOfNullableNaNisNaN()
        {
            Assert.IsTrue(double.IsNaN(new double?[] { double.NaN }.Max().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { float.NaN }.Max().Value));
        }

        [Test]
        public static void MinOfEmptyEnumerableThrows()
        {
            Assert.Throws<InvalidOperationException, double>(() => new double[] { }.Min());
            Assert.Throws<InvalidOperationException, float>(() => new float[] { }.Min());
        }

        [Test]
        public static void MinOfEmptyNullableEnumerableIsNull()
        {
            Assert.AreEqual(null, new double?[] { }.Min());
            Assert.AreEqual(null, new float?[] { }.Min());
        }

        [Test]
        public static void MinOfNaNisNaN()
        {
            Assert.IsTrue(double.IsNaN(new[] { double.NaN }.Min()));
            Assert.IsTrue(float.IsNaN(new[] { float.NaN }.Min()));
        }

        [Test]
        public static void MinOfNaNMinValueIsNaN()
        {
            Assert.IsTrue(double.IsNaN(new[] { double.NaN, double.MinValue }.Min()));
            Assert.IsTrue(float.IsNaN(new[] { float.NaN, float.MinValue }.Min()));
            Assert.IsTrue(double.IsNaN(new[] { double.MinValue, double.NaN }.Min()));
            Assert.IsTrue(float.IsNaN(new[] { float.MinValue, float.NaN }.Min()));
        }

        [Test]
        public static void MinOfNaNMinValueWithNullsIsNaN()
        {
            Assert.IsTrue(double.IsNaN(new double?[] { null, double.NaN, double.MinValue }.Min().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { null, float.NaN, float.MinValue }.Min().Value));
            Assert.IsTrue(double.IsNaN(new double?[] { null, double.MinValue, double.NaN }.Min().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { null, float.MinValue, float.NaN }.Min().Value));

            Assert.IsTrue(double.IsNaN(new double?[] { double.NaN, null, double.MinValue }.Min().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { float.NaN, null, float.MinValue }.Min().Value));
            Assert.IsTrue(double.IsNaN(new double?[] { double.MinValue, null, double.NaN }.Min().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { float.MinValue, null, float.NaN }.Min().Value));

            Assert.IsTrue(double.IsNaN(new double?[] { double.NaN, double.MinValue, null }.Min().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { float.NaN, float.MinValue, null }.Min().Value));
            Assert.IsTrue(double.IsNaN(new double?[] { double.MinValue, double.NaN, null }.Min().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { float.MinValue, float.NaN, null }.Min().Value));
        }

        [Test]
        public static void MinOfNullableNaNAndNullIsNaN()
        {
            Assert.IsTrue(double.IsNaN(new double?[] { double.NaN, null }.Min().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { float.NaN, null }.Min().Value));
            Assert.IsTrue(double.IsNaN(new double?[] { null, double.NaN }.Min().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { null, float.NaN }.Min().Value));
        }

        [Test]
        public static void MinOfNullableNaNisNaN()
        {
            Assert.IsTrue(double.IsNaN(new double?[] { double.NaN }.Min().Value));
            Assert.IsTrue(float.IsNaN(new float?[] { float.NaN }.Min().Value));
        }
    }
}
