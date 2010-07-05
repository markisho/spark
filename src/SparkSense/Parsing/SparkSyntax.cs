﻿using System;
using Spark.Parser.Markup;
using Spark.Parser;
using System.Collections.Generic;
using Spark.Compiler.NodeVisitors;

namespace SparkSense.Parsing
{
    public class SparkSyntax
    {
        public SparkSyntax()
        {

        }

        public IList<Node> ParseNodes(string content)
        {
            var grammar = new MarkupGrammar();
            var result = grammar.Nodes(Source(content));
            return result.Value;
        }

        private static string GetFullElement(string content, int position, int start)
        {
            var nextStart = content.IndexOf('<', position);

            var fullElement = nextStart != -1
                ? content.Substring(start, nextStart - start)
                : content.Substring(start);
            if (!fullElement.Contains(">")) fullElement += "/>";
            else if (!fullElement.Contains("/>")) fullElement = fullElement.Replace(">", "/>");
            return fullElement;
        }

        public Node ParseNode(string content, int position)
        {
            var start = content.LastIndexOf('<', position > 0 ? position - 1 : 0);
            string fullElement = GetFullElement(content, position, start);
            var nodes = ParseNodes(fullElement);

            if (nodes.Count > 1 && nodes[0] is TextNode)
            {
                var firstSpaceAfterStart = content.IndexOf(' ', start) - start;
                var elementWithoutAttributes = content.Substring(start, firstSpaceAfterStart) + "/>";
                nodes = ParseNodes(elementWithoutAttributes);
            }

            return (nodes[0]);
        }

        public Type ParseContext(string content, int position)
        {
            if (content.Substring(position - 1, 1) == " ")
                return typeof(AttributeNode);

            return ParseNode(content, position).GetType();
        }

        public bool IsSparkElementNode(Node inputNode, out Node sparkNode)
        {
            var visitor = new SpecialNodeVisitor(new VisitorContext());
            visitor.Accept(inputNode);
            sparkNode = visitor.Nodes.Count > 0 ? visitor.Nodes[0] : null;
            return sparkNode != null && sparkNode is SpecialNode;
        }

        private static Position Source(string content)
        {
            return new Position(new SourceContext(content));
        }

        private IList<INodeVisitor> BuildNodeVisitors(VisitorContext context)
        {
            return new INodeVisitor[]
                       {
                           new NamespaceVisitor(context),
                           new IncludeVisitor(context),
                           new PrefixExpandingVisitor(context),
                           new SpecialNodeVisitor(context),
                           new CacheAttributeVisitor(context),
                           new ForEachAttributeVisitor(context),
                           new ConditionalAttributeVisitor(context),
                           new OmitExtraLinesVisitor(context),
                           new TestElseElementVisitor(context),
                           new OnceAttributeVisitor(context),
                           new UrlAttributeVisitor(context),
                           //new BindingExpansionVisitor(context)
                       };
        }

    }
}
