using System;

namespace WorldFoundry.Test
{
    public class TypeSurrogate<T> : ITypeSurrogate
    {
        public Type Restore() => typeof(T);
    }
}
