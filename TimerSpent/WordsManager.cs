using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace WordsManagerLib
{
    public class WordsManager
    {
        public List<Line> MainTextLine;
        public string text;
        public PdfReader reader;
        public string pdfPath;

        public WordsManager()
        {
            MainTextLine = new List<Line>();
        }

        public string loadPdf(string path)
        {
            pdfPath = path;
            text = ExtractTextFromPdf(path);
            reader = new PdfReader(path);
            Console.WriteLine("Stockage...");
            if (text.Length < 10)
            {
                throw new Exception("Text length under 10");
            }
            MainTextLine = stockLine(text);
            return text;
        }

        public string ExtractTextFromPdf(string path)
        {
            using (PdfReader reader = new PdfReader(path))
            {
                StringBuilder text = new StringBuilder();

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
                }

                return text.ToString();
            }
        }

        public List<Line> getTextFromRectangle(int x, int y, int w, int h)
        {
            System.util.RectangleJ rect0 = new System.util.RectangleJ(x, y, w, h);
            RenderFilter[] filter = { new RegionTextRenderFilter(rect0) };
            ITextExtractionStrategy strategy;
            StringBuilder sb = new StringBuilder();
            strategy = new FilteredTextRenderListener(new LocationTextExtractionStrategy(), filter);
            sb.AppendLine(PdfTextExtractor.GetTextFromPage(reader, 1, strategy));

            List<Line> line = stockLine(sb.ToString());
            return line;
        }

        public iTextSharp.text.Rectangle getPositionOfString(string searchText)
        {
            //Create an instance of our strategy
            var t = new WordLocationTextExtractionStrategy(searchText);

            //Parse page 1 of the document above
            var ex = PdfTextExtractor.GetTextFromPage(reader, 1, t);
            return t.myPoints[0].Rect;
        }

        // return all the line containing the sentence (stc), if getUnderLine true, return line with it line+1
        public List<Line> getLine(string stc, bool lower = false, bool getUnderLine = false) // TODO All in one line
        {
            if (lower) stc = stc.ToLower();
            List<Line> result = new List<Line>();
            foreach (Line line in MainTextLine)
            {
                if (line.lineContent.ToLower().Contains(stc) && getUnderLine)
                {
                    Line underLine = MainTextLine.Find(i => i.lineNumber == line.lineNumber + 1);
                    line.lineContent = line.lineContent + underLine.lineContent;
                    foreach (Word word in underLine.word)
                    {
                        line.word.Add(word);
                    }
                    result.Add(line);
                }
                else if (line.lineContent.ToLower().Contains(stc) && lower)
                {
                    result.Add(line);
                }
                else if (line.lineContent.Contains(stc))    // For future improvement
                {
                    result.Add(line);
                }
            }
            if (result.Count() == 0)
            {
                throw new Exception("No line found.");
            }
            return result;
        }


        public List<Line> stockLine(string localText, bool saveLine = false)
        {
            List<Line> localLine = new List<Line>();
            string[] lines = localText.Replace("\r", "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    localLine.Add(new Line() { lineContent = lines[i], lineIndex = text.IndexOf(lines[i][0]), lineNumber = i });
                }
                catch (Exception)
                {
                    localLine.Add(new Line() { lineContent = lines[i], lineIndex = 0, lineNumber = i });
                }
                string[] words = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string w in words)
                {
                    localLine[i].word.Add(new Word() { wordName = w, indexInText = localText.IndexOf(w), indexInLine = Array.IndexOf(words, w) });
                }
            }
            if (saveLine)
            {
                MainTextLine = localLine;
            }
            return localLine;
        }

        public void realeasePdf()
        {
            reader = null;
        }
    }

    public class WordLocationTextExtractionStrategy : LocationTextExtractionStrategy
    {
        //Hold each coordinate
        public List<RectAndText> myPoints = new List<RectAndText>();

        //The string that we're searching for
        public String TextToSearchFor { get; set; }

        // Part to find the right word by recording once we have the right first char until the length of the searched text
        public int listen = -1;
        public string word = "";

        //How to compare strings
        public System.Globalization.CompareOptions CompareOptions { get; set; }

        public WordLocationTextExtractionStrategy(String textToSearchFor, System.Globalization.CompareOptions compareOptions = System.Globalization.CompareOptions.None)
        {
            this.TextToSearchFor = textToSearchFor;
            this.CompareOptions = compareOptions;
        }

        //Automatically called for each chunk of text in the PDF
        public override void RenderText(TextRenderInfo renderInfo)
        {
            base.RenderText(renderInfo);

            string text = renderInfo.GetText();

            //See if the current chunk contains the text
            var startPosition = System.Globalization.CultureInfo.CurrentCulture.CompareInfo.IndexOf(renderInfo.GetText(), this.TextToSearchFor, this.CompareOptions);

            if (listen >= this.TextToSearchFor.Count())
            {
                if (this.TextToSearchFor == word)
                {
                    //Grab the individual characters
                    var chars = renderInfo.GetCharacterRenderInfos().Skip(startPosition).Take(this.TextToSearchFor.Length).ToList();

                    //Grab the first and last character
                    var firstChar = chars.First();
                    var lastChar = chars.Last();


                    //Get the bounding box for the chunk of text
                    var bottomLeft = firstChar.GetDescentLine().GetStartPoint();
                    var topRight = lastChar.GetAscentLine().GetEndPoint();

                    //Create a rectangle from it
                    var rect = new iTextSharp.text.Rectangle(
                                                            bottomLeft[Vector.I1],
                                                            bottomLeft[Vector.I2],
                                                            topRight[Vector.I1],
                                                            topRight[Vector.I2]
                                                            );

                    //Add this to our main collection
                    this.myPoints.Add(new RectAndText(rect, word));
                    listen = -1;
                    word = "";
                    return;
                }
                listen = -1;
                word = "";
            }
            else if (listen > -1)
            {
                word += renderInfo.GetText();
                listen += renderInfo.GetText().Count();
            }
            else if (this.TextToSearchFor[0] == renderInfo.GetText()[0])
            {
                listen = 0;
                word += renderInfo.GetText();
                listen += renderInfo.GetText().Count();
            }
            else
            {
                return;
            }
        }
    }

    public class RectAndText
    {
        public iTextSharp.text.Rectangle Rect;
        public string Text;
        public RectAndText(iTextSharp.text.Rectangle rect, string text)
        {
            this.Rect = rect;
            this.Text = text;
        }
    }

    public class Line
    {
        public string lineContent = "";
        public int lineIndex = -1;
        public int lineNumber = -1;
        public List<Word> word;

        public Line()
        {
            word = new List<Word>();
        }
    }

    public class Word   // Useful for quick check of word
    {
        public string wordName = "";
        public int indexInText = -1;    // index of the first letter among the other letter
        public int indexInLine = -1;   // index of the word in the line
    }

}
