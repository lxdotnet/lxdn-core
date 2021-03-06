
using System;

namespace Lxdn.Core.Validation
{
    public class OperatorModelValidationException : ApplicationException
    {
        public OperatorModelValidationException(ValidationResult result) : base(result.Message)
        {
            this.Result = result;
        }

        public ValidationResult Result { get; private set; }
    }
}