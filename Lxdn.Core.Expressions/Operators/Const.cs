using System;
using System.Linq.Expressions;
using Lxdn.Core.Expressions.Operators.Models;
using Lxdn.Core.Extensions;

namespace Lxdn.Core.Expressions.Operators
{
    public class Const : Operator
    {
        private readonly Func<Expression> create;

        public Const(ConstModel constant, Type inferred = null)
        {
            this.create = () => "null".Equals(constant.Value)
                ? Expression.Constant(null, inferred ?? typeof(string))
                : constant.Value.ToExpression(inferred ?? typeof(string));
        }

        public Const(string value, Type inferred) : this(new ConstModel { Value = value }, inferred) {}

        protected internal override Expression Create()
        {
            return this.create();
        }
    }
}