using System;
using AutoAdapter;

namespace AutoAdapterUnitTests.Resources
{
    public delegate bool AdaptedDelegateHandler(IChildAdapter child);

    public interface IAdapterWithChildAdapter
    {
        IChildAdapter Child { get; }

        IChildAdapter[] Children { get; }

        bool TryGetChild(out IChildAdapter child);


        bool TryGetChildren(out IChildAdapter[] children);

        void ActionParameter(Action<IChildAdapter> action);

        void FuncParameter(Func<IChildAdapter> func);

        bool PredicateParameter(Predicate<IChildAdapter> predicate);

        bool Check(AdaptedDelegateHandler handler);
    }
}