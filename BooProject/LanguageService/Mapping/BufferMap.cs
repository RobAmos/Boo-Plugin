﻿using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Hill30.BooProject.LanguageService.Mapping
{
    public class BufferMap
    {
        private int[][] positionMap;
        public ITextSnapshot CurrentSnapshot { get; private set; }
        public string FilePath { get; private set; }


        public void Map(ITextBuffer buffer, int tabSize)
        {
            CurrentSnapshot = buffer.CurrentSnapshot;
            var doc = buffer.Properties[typeof(ITextDocument)] as ITextDocument;
            if (doc != null)
                FilePath = doc.FilePath;

            var source = CurrentSnapshot.GetText();
            var sourcePos = 0;
            var mappedPos = 0;
            var mappers = new List<int[]>();
            var positionList = new List<int>();
            foreach (var c in source)
            {
                if (c == '\t')
                    while (mappedPos % tabSize < tabSize - 1)
                    {
                        positionList.Add(sourcePos);
                        mappedPos++;
                    }
                positionList.Add(sourcePos++);
                mappedPos++;
                if (c == '\n')
                {
                    mappers.Add(positionList.ToArray());
                    positionList.Clear();
                    sourcePos = 0;
                    mappedPos = 0;
                }
            }
            positionList.Add(sourcePos); // to map the <EOL> token
            mappers.Add(positionList.ToArray());
            positionMap = mappers.ToArray();
        }

        public struct BufferPoint
        {
            public int Line;
            public int Column;
        }

        public BufferPoint MapPosition(int line, int pos)
        {
            if (line == -1)
                line = positionMap.Length;
            if (pos == -1)
                pos = positionMap[line - 1].Length;
            return new BufferPoint {Line = line - 1, Column = positionMap[line - 1][pos - 1]};
        }
    }
}
