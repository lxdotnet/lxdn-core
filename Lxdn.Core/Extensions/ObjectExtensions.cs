using System;
using System.Linq;
using System.Dynamic;
using System.Reflection;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Lxdn.Core.Basics;

namespace Lxdn.Core.Extensions
{
    public static class ObjectExtensions
    {
        public static bool HasDefaultValue<TInput>(this TInput input)
        {
            return Equals(input, default(TInput));
        }

        public static TReturn IfExists<TInput, TReturn>(this TInput input, Func<TInput, TReturn> accessor)
        {
            if (input.HasDefaultValue())
            {
                // not sure; check thoroughly for performance pitfalls

                //return typeof(TReturn).AsArgumentsOf(typeof(Task<>))
                //    .IfHasValue(args => args.Single())
                //    .IfExists(resultType => (TReturn)typeof(Task).GetMethod("FromResult")?.MakeGenericMethod(resultType)
                //        .Invoke(null, new[] { resultType.DefaultValue() }));

                // if we are returning a Task<SomeType>, return Task of SomeType.DefaultValue to enable awaiting of it:
                if (typeof(TReturn).IsGenericType && typeof(TReturn).GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var underlyingType = typeof(TReturn).GetGenericArguments().Single();
                    return (TReturn)typeof(Task).GetMethod("FromResult")?.MakeGenericMethod(underlyingType)
                        .Invoke(null, new[] { underlyingType.DefaultValue() });
                }

                return (TReturn)typeof(TReturn).DefaultValue();
            }

            return accessor(input);
        }

        public static TInput IfExists<TInput>(this TInput input) => input.IfExists(something => something); // default value check

        public static void IfExists<TInput>(this TInput input, Action<TInput> action)
        {
            // http://stackoverflow.com/questions/5340817/what-should-i-do-about-possible-compare-of-value-type-with-null
            if (!input.HasDefaultValue())
            {
                action(input);
            }
        }

        public static TValue ThrowIfDefault<TValue, TException>(this TValue value, Func<TException> exception)
            where TException : Exception
        {
            return value.ThrowIf(v => v.HasDefaultValue(), v => exception());
        }

        public static TValue ThrowIfDefault<TValue>(this TValue value)
        {
            return value.ThrowIfDefault(() => new InvalidOperationException());
        }

        public static TValue ThrowIf<TValue, TException>(this TValue value, Func<TValue, bool> condition, Func<TValue, TException> exception)
            where TException : Exception
        {
            if (condition(value))
                throw exception(value);
            return value;
        }

        public static object ChangeType(this object obj, Type target, CultureInfo culture)
        {
            if (obj == null)
                return target.DefaultValue();

            var source = obj.GetType();

            if (target.IsAssignableFrom(source))
                return obj;

            if (target.IsGenericType && typeof(Nullable<>) == target.GetGenericTypeDefinition())
            {
                var underlyingType = Nullable.GetUnderlyingType(target);
                return obj.ChangeType(underlyingType, culture);
            }

            if (obj is IConvertible && typeof(IConvertible).IsAssignableFrom(target) && Type.GetTypeCode(target) != TypeCode.Object)
            {
                if (typeof(bool) == target && typeof(string) == source)
                {
                    if (new[] { bool.FalseString, bool.TrueString }.Any(s => string.Equals(s, (string)obj, StringComparison.InvariantCultureIgnoreCase)))
                        return ((string)obj).To<bool>();

                    return Convert.ToInt32(obj) != default(int);
                }

                if (!target.IsEnum/*only known exception*/)
                    return Convert.ChangeType(obj, target, culture);

                // here when target is en enum:
                if (typeof(string) == source)
                {
#if NETCORE
                    if (Enum.TryParse(target, (string)obj, true, out object member))
                        return member;

                    return target.GetMembers()
                        .FirstOrDefault(candidate => candidate.GetCustomAttribute<EnumMemberAttribute>()
                            .IfExists(attribute => string.Equals(attribute.Value, (string)obj, StringComparison.InvariantCultureIgnoreCase)))
                        .IfExists(member => Enum.Parse(target, member.Name, true))
                        ?? throw new ArgumentOutOfRangeException(nameof(obj), "The following value is out of range: " + obj);
#else
                    return Enum.Parse(target, (string)obj, true); // .net standard cannot .TryParse with case-insensitive flag
#endif
                }

                return Enum.ToObject(target, Convert.ToInt32(obj));
            }

            // otherwise try an indirect conversion via invariant string:
            var formattable = obj as IFormattable;
            if (formattable != null)
            {
                return formattable.ToString("", CultureInfo.InvariantCulture).To(target);
            }

            return obj.ToString().To(target);
        }

        public static object ChangeType(this object obj, Type target) => obj.ChangeType(target, CultureInfo.InvariantCulture);

        public static TResult ChangeType<TResult>(this object obj, CultureInfo culture)
            => (TResult)obj.ChangeType(typeof(TResult), culture);

        public static TResult ChangeType<TResult>(this object obj)
            => (TResult)obj.ChangeType(typeof(TResult), CultureInfo.InvariantCulture);

        public static TTarget SetValue<TTarget>(this TTarget target, PropertyInfo property, object value)
            where TTarget : class
        {
            property.SetValue(target, value);
            return target;
        }

        public static IEnumerable<TItem> Once<TItem>(this TItem item) => Enumerable.Repeat(item, 1);

        public static bool IsOneOf<TItem>(this TItem item, params TItem[] values)
            => values.Any(value => Equals(value, item));

        public static bool IsOneOf<TItem>(this TItem item, IEnumerable<TItem> values)
            => IsOneOf(item, values.ToArray());

        public static bool NotIn<TItem>(this TItem item, params TItem[] values)
            => values.All(value => !Equals(value, item));

        public static Dictionary<string, object> ToDictionary(this object o)
            => o.ToDictionary(StringComparer.InvariantCultureIgnoreCase);

        public static Dictionary<string, object> ToDictionary(this object o, IEqualityComparer<string> comparer)
        {
            if (o == null)
                return new Dictionary<string, object>();

            return o.GetDynamicMetaObject()
                    .IfExists(dynamic => dynamic
                    .GetDynamicMemberNames()
                    .ToDictionary(name => name, name => ((dynamic)o)[name], comparer))
                   ??
                   o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.HasPublicGetter())
                    .ToDictionary(property => property.Name, property => property.GetValue(o), comparer);
        }

        public static object Call(this object item, string method, params object[] parameters) => item.GetType().GetMethod(method).Invoke(item, parameters);

        //public static IPropertyAccessor PropertyOf(this object item, string name, bool caseSensitive = false, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        //{
        //    if (item == null)
        //        throw new ArgumentNullException(nameof(item));

        //    return item.GetType().GetProperties(flags)
        //        .SingleOrDefault(candidate => 0 == string.Compare(candidate.Name, name, !caseSensitive, CultureInfo.InvariantCulture))
        //        .IfExists(propery => new Property(propery))
        //        ?.CreateAccessor(item);
        //}

        

        public static int HashUsing<TItem>(this TItem item, params Func<TItem, object>[] properties) 
            => unchecked(properties.Aggregate((int)2166136261, (hash, propertyOf) 
                => Hash.Combine(hash, propertyOf(item)?.GetHashCode() ?? 0)));

        private static TInput InterpretAs<TInput>(this TInput obj, Expression<Func<TInput, DateTime>> lambda, DateTimeKind kind)
            where TInput : class
        {
            var property = (lambda.Body as MemberExpression).IfExists(member => member.Member as PropertyInfo)
                ?? throw new ArgumentException("An expression used in the lambda must be a property expression");

            var dateTime = DateTime.SpecifyKind((DateTime)property.GetValue(obj), kind);
            return obj.SetValue(property, dateTime);
        }

        public static TInput InterpretAsUtcTime<TInput>(this TInput obj, Expression<Func<TInput, DateTime>> dateTime) where TInput : class 
            => InterpretAs(obj, dateTime, DateTimeKind.Utc);

        public static TInput InterpretAsLocalTime<TInput>(this TInput obj, Expression<Func<TInput, DateTime>> dateTime) where TInput : class 
            => InterpretAs(obj, dateTime, DateTimeKind.Local);

        public static DynamicMetaObject GetDynamicMetaObject(this object o)
            => (o as IDynamicMetaObjectProvider)?.GetMetaObject(Expression.Constant(o));
    }
}
