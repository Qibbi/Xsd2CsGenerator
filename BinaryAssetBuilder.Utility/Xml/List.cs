using System.Xml;
using System.Xml.XPath;

namespace Xml;

public sealed class List
{
    private XPathNodeIterator _currentIterator;
    private readonly XPathNodeIterator _iterator;
    private readonly XmlNamespaceManager _namespaceManager;

    public int Count => _currentIterator.Count;

    public List(XPathNodeIterator iterator, XmlNamespaceManager namespaceManager)
    {
        _currentIterator = iterator;
        _iterator = iterator.Clone();
        _namespaceManager = namespaceManager;
    }

    public Node? NextNode()
    {
        if (_currentIterator.MoveNext())
        {
            return new Node(_currentIterator.Current!.CreateNavigator(), _namespaceManager);
        }
        return null;
    }

    public void Reset()
    {
        _currentIterator = _iterator.Clone();
    }
}
