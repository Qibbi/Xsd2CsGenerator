using System.Xml;
using System.Xml.XPath;

namespace Xml;

public sealed class Node
{
    private readonly XPathNavigator? _navigator;
    private readonly string? _defaultValue;
    private readonly XmlNamespaceManager? _namespaceManager;

    public Value? Value
    {
        get
        {
            if (_navigator is not null)
            {
                return new Value(_navigator.Value);
            }
            if (_defaultValue is not null)
            {
                return new Value(_defaultValue);
            }
            return null;
        }
    }

    public Node(XPathNavigator navigator, XmlNamespaceManager namespaceManager)
    {
        _navigator = navigator;
        _namespaceManager = namespaceManager;
    }

    public Node(string defaultValue)
    {
        _defaultValue = defaultValue;
    }

    public List? GetChildNodes(string name)
    {
        if (_navigator is null)
        {
            return null;
        }
        if (name is null)
        {
            return new List(_navigator.SelectChildren(XPathNodeType.Element), _namespaceManager!);
        }
        return new List((XPathNodeIterator)_navigator.Evaluate($"ea:{name}", _namespaceManager), _namespaceManager!);
    }

    public Node? GetChildNode(string name, string defaultValue)
    {
        if (_navigator?.MoveToChild(name, _navigator.NamespaceURI) ?? false)
        {
            Node result = new(_navigator.CreateNavigator(), _namespaceManager!);
            _navigator.MoveToParent();
            return result;
        }
        if (defaultValue is null)
        {
            return null;
        }
        return new Node(defaultValue);
    }

    public Value? GetAttributeValue(string name, string defaultValue)
    {
        if (_navigator?.MoveToAttribute(name, string.Empty) ?? false)
        {
            Value result = new(_navigator.Value);
            _navigator.MoveToParent();
            return result;
        }
        if (defaultValue is null)
        {
            return null;
        }
        return new Value(defaultValue);
    }
}
