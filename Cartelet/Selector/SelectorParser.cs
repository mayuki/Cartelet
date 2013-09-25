using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    public partial class SelectorParser
    {
        public Selector Parse()
        {
            Selector();
            return this.Root as Selector;
        }

        public Boolean Selector()
        {
            // selector
            //   : simple_selector_sequence [ combinator simple_selector_sequence ]*
            // ;
            return Production("Selector").Execute(() =>
            {
                return Expect(SimpleSelectorSequence)
                    && ExpectZeroOrMore(() => Expect(Combinator) && Expect(SimpleSelectorSequence));
            });
        }

        public Boolean SimpleSelectorSequence()
        {
            // simple_selector_sequence
            //   : [ type_selector | universal ]
            //     [ HASH | class | attrib | pseudo | negation ]*
            //   | [ HASH | class | attrib | pseudo | negation ]+
            //   ;

            return Production("SimpleSelectorSequence").Execute(() =>
            {
                return Expect(() => (
                           Expect(() => TypeSelector() || Universal())
                        && ExpectZeroOrMore(() => (Expect(Hash_Id, Class, Attrib, Negation, Pseudo)))
                       ), () => ExpectZeroOrMore(() => (Expect(Hash_Id, Class, Attrib, Negation, Pseudo)))); // negation と pseudo を逆にしないと not が引っかかる(上も)
            });
        }

        public Boolean Hash_Id()
        {
            // HASH (ID Selector)
            return Production("Id").Execute(() =>
            {
                return Expect(Capture(Hash));
            });
        }

        public Boolean Combinator()
        {
            // combinator
            //   /* combinators can be surrounded by whitespace */
            //   : PLUS S* | GREATER S* | TILDE S* | S+
            //   ;
            return Production("Combinator").Execute(() =>
            {
                return Expect(Capture(() =>
                    (Expect(Plus, Greater, Tilde) && ExpectZeroOrMore(Space)) || Expect(() => ExpectOneOrMore(Space))));
            });
        }

        public Boolean TypeSelector()
        {
            // type_selector
            //   : [ namespace_prefix ]? element_name
            //   ;
            return Production("TypeSelector").Execute(() =>
            {
                return ExpectZeroOrOne(NamespacePrefix)
                    && Expect(ElementName);
            });
        }

        public Boolean NamespacePrefix()
        {
            // namespace_prefix
            //   : [ IDENT | '*' ]? '|'
            //   ;
            return Production("NamespacePrefix").Execute(() =>
            {
                return ExpectZeroOrOne(Capture(() => Expect(Ident, Char('*'))))
                    && Expect(Char('|'));
            });
        }

        public Boolean ElementName()
        {
            // element_name
            //   : IDENT
            //   ;
            return Production("ElementName").Execute(() =>
            {
                return Expect(Capture(Ident));
            });
        }

        public Boolean Universal()
        {
            // universal
            //   : [ namespace_prefix ]? '*'
            //   ;
            return Production("UniversalSelector").Execute(() =>
            {
                return ExpectZeroOrOne(NamespacePrefix)
                    && Expect(Char('*'));
            });
        }

        public Boolean Class()
        {
            // class
            //   : '.' IDENT
            //   ;
            return Production("Class").Execute(() =>
            {
                return Expect(Char('.'))
                    && Expect(Capture(Ident));
            });
        }

        public Boolean Attrib()
        {
            // attrib
            //   : '[' S* [ namespace_prefix ]? IDENT S*
            //         [ [ PREFIXMATCH |
            //             SUFFIXMATCH |
            //             SUBSTRINGMATCH |
            //             '=' |
            //             INCLUDES |
            //             DASHMATCH ] S* [ IDENT | STRING ] S*
            //         ]? ']'
            //   ;
            return Production("Attrib").Execute(() =>
            {
                return Expect(Char('['))
                    && ExpectZeroOrMore(Space)
                    && ExpectZeroOrOne(NamespacePrefix)
                    && Expect(Capture(Ident))
                    && ExpectZeroOrMore(Space)
                    && ExpectZeroOrOne(() =>
                           Capture(() => Expect(PrefixMatch, SuffixMatch, SubstringMatch, Char('='), Includes, DashMatch))()
                           && ExpectZeroOrMore(Space)
                           && Expect(Capture(() => Expect(Ident, String)))
                           && ExpectZeroOrMore(Space)
                       )
                    && Expect(Char(']'));
            });
        }

        public Boolean Pseudo()
        {
            // pseudo
            //   /* '::' starts a pseudo-element, ':' a pseudo-class */
            //   /* Exceptions: :first-line, :first-letter, :before and :after. */
            //   /* Note that pseudo-elements are restricted to one per selector and */
            //   /* occur only in the last simple_selector_sequence. */
            //   : ':' ':'? [ IDENT | functional_pseudo ]
            //   ;
            return Production("Pseudo").Execute(() =>
            {
                return Expect(Char(':'))
                    && ExpectZeroOrOne(Char(':'))
                    && Expect(FunctionalPseudo, Capture(Ident)); // 逆にしないとIdentで止まってしまう
            });
        }

        public Boolean FunctionalPseudo()
        {
            // functional_pseudo
            //   : FUNCTION S* expression ')'
            //   ;
            return Production("FunctionalPseudo").Execute(() =>
            {
                return Expect(Capture(Function))
                    && ExpectZeroOrMore(Space)
                    && Expect(Expression)
                    && Expect(Char(')'));
            });
        }

        public Boolean Expression()
        {
            // expression
            //   /* In CSS3, the expressions are identifiers, strings, */
            //   /* or of the form "an+b" */
            //   : [ [ PLUS | '-' | DIMENSION | NUMBER | STRING | IDENT ] S* ]+
            //   ;
            return Production("Expression").Execute(() =>
            {
                return ExpectOneOrMore(Capture(() => Expect(Plus, Char('-'), Dimension, Number, String, Ident) && ExpectZeroOrMore(Space)));
            });
        }

        public Boolean Negation()
        {
            // negation
            //   : NOT S* negation_arg S* ')'
            //   ;
            return Production("Negation").Execute(() =>
            {
                return Expect(Not)
                    && ExpectZeroOrMore(Space)
                    && Expect(NegationArg)
                    && ExpectZeroOrMore(Space)
                    && Expect(Char(')'));
            });
        }

        public Boolean NegationArg()
        {
            // negation_arg
            //   : type_selector | universal | HASH | class | attrib | pseudo
            //   ;
            return Production("NegationArg").Execute(() =>
            {
                return Expect(TypeSelector, Universal, Hash, Class, Attrib, Pseudo);
            });
        }

        public Boolean Nth()
        {
            // nth
            //   : S* [ ['-'|'+']? INTEGER? {N} [ S* ['-'|'+'] S* INTEGER ]? |
            //          ['-'|'+']? INTEGER | {O}{D}{D} | {E}{V}{E}{N} ] S*
            //   ;
            return Production("Nth").Execute(() =>
            {
                return ExpectZeroOrMore(Space)
                    && Expect(
                        () => ExpectZeroOrOne(Char(new[] { '-', '+' }))
                           && ExpectOneOrMore(CharRange('0', '9'))
                           && Expect(N)
                           && ExpectZeroOrOne(() => ExpectZeroOrMore(Space) && Char(new[] { '-', '+' })() && ExpectZeroOrMore(Space) && ExpectOneOrMore(CharRange('0', '9'))),
                        () => ExpectZeroOrOne(Char(new[] { '-', '+' }))
                           && ExpectOneOrMore(CharRange('0', '9')),
                        () => Expect(O) && Expect(D) && Expect(D),
                        () => Expect(E) && Expect(V) && Expect(E) && Expect(N)
                    )
                    && ExpectZeroOrMore(Space);
            });
        }

        public Production Root { get; set; }

        public Production Production(String name)
        {
            switch (name)
            {
                case "Selector":
                    return new Selector(name, this);
                case "SimpleSelectorSequence":
                    return new SimpleSelectors(name, this);
                case "Class":
                    return new ClassSelector(name, this);
                case "Id":
                    return new IdSelector(name, this);
                case "Attrib":
                    return new AttributeSelector(name, this);
                case "Pseudo":
                    return new PseudoSelector(name, this);
                case "FunctionalPseudo":
                    return new FunctionalPseudoSelector(name, this);
                case "TypeSelector":
                    return new TypeSelector(name, this);
                case "UniversalSelector":
                    return new UniversalSelector(name, this);
                case "Combinator":
                    return new Combinator(name, this);
                case "Expression":
                    return new Expression(name, this);
                case "Negation":
                    return new Negation(name, this);
                default:
                    return new Production(name, this);
            }
        }
    }
}
