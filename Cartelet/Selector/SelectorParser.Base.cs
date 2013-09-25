using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    public partial class SelectorParser
    {
        private String _selector;
        public Int32 Index { get; set; }
        public Production CurrentProduction { get; set; }

        public SelectorParser(String selector)
        {
            _selector = selector;
            Index = 0;
        }

        public Char ReadNext()
        {
            return _selector[Index++];
        }

        public Char PeekNext()
        {
            return _selector[Index + 1];
        }

        public Boolean HasNext()
        {
            return Index <= _selector.Length - 1;
        }

        public Func<Boolean> Chars(String s)
        {
            return () =>
            {
                for (var i = 0; i < s.Length; i++)
                {
                    if (!HasNext())
                        return false;

                    var nextChar = System.Char.ToLower(ReadNext());
                    if (s[i] != nextChar)
                        return false;
                }
                return true;
            };
        }

        public Func<Boolean> Char(Char c, Boolean isNegative = false)
        {
            return () =>
            {
                if (!HasNext())
                    return false;

                var nextChar = System.Char.ToLower(ReadNext());
                return isNegative
                            ? (c != nextChar)
                            : (c == nextChar);
            };
        }

        public Func<Boolean> Char(Char[] c, Boolean isNegative = false)
        {
            return () =>
            {
                if (!HasNext())
                    return false;

                var nextChar = System.Char.ToLower(ReadNext());
                return isNegative
                            ? (c.All(x => x != nextChar)) // 何も含まない
                            : (c.Any(x => x == nextChar)); // どれか含む
            };
        }
        public Func<Boolean> CharRange(Char start, Char end, Boolean isNegative = false)
        {
            return () =>
            {
                if (!HasNext())
                    return false;

                var nextChar = System.Char.ToLower(ReadNext());
                var isContain = (start <= nextChar && end >= nextChar);
                return isNegative
                            ? !isContain
                            : isContain;
            };
        }

        public Boolean Nmstart()
        {
            // nmstart   [_a-z]|{nonascii}|{escape}
            return Expect(Char('_'), CharRange('a', 'z'), NonAscii, Escape);
        }
        public Boolean Nmchar()
        {
            // nmchar    [_a-z0-9-]|{nonascii}|{escape}
            return Expect(Char('_'), CharRange('a', 'z'), Char('-'), Num, NonAscii, Escape);
        }
        public Boolean Num()
        {
            // num       [0-9]+|[0-9]*\.[0-9]+
            // TODO:
            return ExpectOneOrMore(CharRange('0', '9'));
        }
        public Boolean NonAscii()
        {
            // nonascii  [^\0-\177]
            return CharRange('\0', '\xB1', true)();
        }
        public Boolean Unicode()
        {
            // unicode   \\[0-9a-f]{1,6}(\r\n|[ \n\r\t\f])?
            return Expect(Char('\\')) && ExpectOneOrMore(() => Expect(CharRange('0', '9'), CharRange('a', 'f')), 6)
                && ExpectZeroOrOne(() => Expect(Chars("\r\n")) || Expect(Char(' '), Char('\n'), Char('\t'), Char('\f')));
        }

        public Boolean Escape()
        {
            // escape    {unicode}|\\[^\n\r\f0-9a-f]
            return Expect(Unicode, () => Expect(Char('\\')) && Expect(() =>
            {
                if (!HasNext())
                    return false;
                var nextChar = System.Char.ToLower(ReadNext());
                return !(
                    nextChar == '\n'
                    || nextChar == '\r'
                    || nextChar == '\f'
                    || (nextChar >= '0' && nextChar <= '9')
                    || (nextChar >= 'a' && nextChar <= 'f')
                  );
            }));
        }

        public Boolean Ident()
        {
            // ident     [-]?{nmstart}{nmchar}*
            return ExpectZeroOrOne(Char('-'))
                && Expect(Nmstart)
                && ExpectZeroOrMore(Nmchar);
        }

        public Boolean Space()
        {
            // [ \t\r\n\f]+     return S;
            return ExpectOneOrMore(() => Expect(Char(' '), Char('\t'), Char('\n'), Char('\f')));
        }

        public Boolean W()
        {
            // w         [ \t\r\n\f]*
            return ExpectZeroOrMore(() => Expect(Char(' '), Char('\t'), Char('\n'), Char('\f')));
        }

        public Boolean Plus()
        {
            // {w}"+"           return PLUS;
            return W() && Expect(Char('+'));
        }

        public Boolean Greater()
        {
            // {w}">"           return GREATER;
            return W() && Expect(Char('>'));
        }

        public Boolean Tilde()
        {
            // {w}"~"           return TILDE;
            return W() && Expect(Char('~'));
        }

        public Boolean Comma()
        {
            // {w}","           return COMMA;
            return W() && Expect(Char(','));
        }

        public Boolean Hash()
        {
            // "#"{name}        return HASH;
            return Expect(Char('#'))
                && Expect(Name);
        }

        public Boolean Name()
        {
            // name      {nmchar}+
            return ExpectOneOrMore(Nmchar);
        }

        public Boolean Dimension()
        {
            return false;
        }

        public Boolean Number()
        {
            // {num}            return NUMBER;
            return Num();
        }

        public Boolean String()
        {
            // string    {string1}|{string2}
            return Expect(String1, String2);
        }

        public Boolean String1()
        {
            // string1   \"([^\n\r\f\\"]|\\{nl}|{nonascii}|{escape})*\"
            return Expect(Char('"'))
                && ExpectZeroOrMore(() => Expect(
                        Char(new [] { '\n', '\r', '\f', '\\', '"' }, true),
                        () => Expect(Char('\\')) && Expect(Nl),
                        NonAscii,
                        Escape
                    ))
                && Expect(Char('"'));
        }

        public Boolean String2()
        {
            // string2   \'([^\n\r\f\\']|\\{nl}|{nonascii}|{escape})*\'
            return Expect(Char('\''))
                && ExpectZeroOrMore(() => Expect(
                        Char(new[] { '\n', '\r', '\f', '\\', '\'' }, true),
                        () => Expect(Char('\\')) && Expect(Nl),
                        NonAscii,
                        Escape
                    ))
                && Expect(Char('\''));
        }

        public Boolean Nl()
        {
            // nl        \n|\r\n|\r|\f
            return Expect(Char('\n'), () => Expect(Char('\r'), Char('\n')), Char('\r'), Char('\f'));
        }

        public Boolean Function()
        {
            // {ident}"("       return FUNCTION;
            return Expect(Ident)
                && Expect(Char('('));
        }

        public Boolean D()
        {
            // D         d|\\0{0,4}(44|64)(\r\n|[ \t\r\n\f])?
            return Expect(Char('d'), Char('D'),
                          () => Char('\\')()
                                  && ExpectZeroOrMore(Char('0'), 4)
                                  && Expect(Chars("44"), Chars("64"))
                                  && ExpectZeroOrOne(() => Expect(Char(' '), Char('\r'), Char('\n'), Char('\r'), Char('\f'))));
        }
        public Boolean E()
        {
            // E         e|\\0{0,4}(45|65)(\r\n|[ \t\r\n\f])?
            return Expect(Char('e'), Char('E'),
                          () => Char('\\')()
                                  && ExpectZeroOrMore(Char('0'), 4)
                                  && Expect(Chars("45"), Chars("65"))
                                  && ExpectZeroOrOne(() => Expect(Char(' '), Char('\r'), Char('\n'), Char('\r'), Char('\f'))));
        }
        public Boolean N()
        {
            // N         n|\\0{0,4}(4e|6e)(\r\n|[ \t\r\n\f])?|\\n
            return Expect(Char('n'), Char('N'),
                          () => Char('\\')()
                                  && ExpectZeroOrMore(Char('0'), 4)
                                  && Expect(Chars("4e"), Chars("6e"))
                                  && ExpectZeroOrOne(() => Expect(Char(' '), Char('\r'), Char('\n'), Char('\r'), Char('\f'))),
                          (Chars("\\n")), Chars("\\N"));
        }
        public Boolean O()
        {
            // O         o|\\0{0,4}(4f|6f)(\r\n|[ \t\r\n\f])?|\\o
            return Expect(Char('o'), Char('O'),
                          () => Char('\\')()
                                  && ExpectZeroOrMore(Char('0'), 4)
                                  && Expect(Chars("4f"), Chars("6f"))
                                  && ExpectZeroOrOne(() => Expect(Char(' '), Char('\r'), Char('\n'), Char('\r'), Char('\f'))),
                          (Chars("\\o")), Chars("\\O"));
        }
        public Boolean T()
        {
            // T         t|\\0{0,4}(54|74)(\r\n|[ \t\r\n\f])?|\\t
            return Expect(Char('t'), Char('T'),
                          () => Char('\\')()
                                  && ExpectZeroOrMore(Char('0'), 4)
                                  && Expect(Chars("54"), Chars("74"))
                                  && ExpectZeroOrOne(() => Expect(Char(' '), Char('\r'), Char('\n'), Char('\r'), Char('\f'))),
                          (Chars("\\t")), Chars("\\T"));
        }
        public Boolean V()
        {
            // V         v|\\0{0,4}(58|78)(\r\n|[ \t\r\n\f])?|\\v
            return Expect(Char('v'), Char('V'),
                          () => Char('\\')()
                                  && ExpectZeroOrMore(Char('0'), 4)
                                  && Expect(Chars("58"), Chars("78"))
                                  && ExpectZeroOrOne(() => Expect(Char(' '), Char('\r'), Char('\n'), Char('\r'), Char('\f'))),
                          (Chars("\\v")), Chars("\\V"));
        }

        public Boolean Not()
        {
            // ":"{N}{O}{T}"("  return NOT;
            return Expect(Char(':')) && Expect(N) && Expect(O) && Expect(T) && Expect(Char('('));
        }

        public Boolean PrefixMatch()
        {
            // "^="             return PREFIXMATCH;
            return Expect(Chars("^="));
        }
        public Boolean SuffixMatch()
        {
            // "$="             return SUFFIXMATCH;
            return Expect(Chars("$="));
        }
        public Boolean SubstringMatch()
        {
            // "*="             return SUBSTRINGMATCH;
            return Expect(Chars("*="));
        }
        public Boolean Includes()
        {
            // "~="             return INCLUDES;
            return Expect(Chars("~="));
        }
        public Boolean DashMatch()
        {
            // "|="             return DASHMATCH;
            return Expect(Chars("|="));
        }

        public Boolean Expect(params Func<Boolean>[] nextAlternates)
        {
            var origIndex = Index;
            foreach (var next in nextAlternates)
            {
                if (next())
                {
                    return true;
                }

                // rewind
                Index = origIndex;
            }
            return false;
        }
        public Boolean ExpectZeroOrMore(Func<Boolean> next, Int32 maxCount = Int32.MaxValue)
        {
            var origIndex = Index;
            var isSuccess = true;
            var count = 0;
            while (isSuccess && count++ < maxCount)
            {
                isSuccess = next();
                if (isSuccess)
                {
                    origIndex = Index;
                }
                else
                {
                    // rewind if no more next
                    Index = origIndex;
                }
            }
            return true;
        }
        public Boolean ExpectOneOrMore(Func<Boolean> next, Int32 maxCount = Int32.MaxValue)
        {
            var isSuccess = true;
            var origIndex = Index;
            // One
            isSuccess &= next();

            // or More
            if (isSuccess)
            {
                var count = 1;
                var moreIsSuccess = true;
                while (moreIsSuccess && count++ < maxCount)
                {
                    origIndex = Index;
                    moreIsSuccess &= next();
                    if (!moreIsSuccess)
                    {
                        // rewind
                        Index = origIndex;
                    }
                }
            }
            else
            {
                // rewind
                Index = origIndex;
            }

            return isSuccess;
        }
        public Boolean ExpectZeroOrOne(Func<Boolean> next)
        {
            var origIndex = Index;
            if (!next())
            {
                // rewind if zero
                Index = origIndex;
            }
            return true;
        }


        public Func<Boolean> Capture(Func<Boolean> func)
        {
            return () =>
            {
                var origIndex = Index;
                var result = func();
                if (result)
                {
                    var capturedString = _selector.Substring(origIndex, Index - origIndex);
                    CurrentProduction.Captures.Add(capturedString);
                }
                return result;
            };
        }
    }
}
