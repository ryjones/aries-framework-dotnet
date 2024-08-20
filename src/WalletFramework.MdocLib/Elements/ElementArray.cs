using PeterO.Cbor;
using WalletFramework.Core.Functional;
using static WalletFramework.MdocLib.Elements.Element;

namespace WalletFramework.MdocLib.Elements;

public readonly struct ElementArray
{
    public List<Element> Value { get; }

    private ElementArray(List<Element> value) => Value = value;
    
    public Element this[ushort index] => Value[index];

    internal static Validation<ElementArray> ValidElementArray(CBORObject cbor) =>
        cbor.Values
            .Select(ValidElement)
            .TraverseAll(value => value)
            .OnSuccess(values => new ElementArray(values.ToList()));
}
