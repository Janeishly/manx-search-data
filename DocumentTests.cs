﻿using Manx_Search_Data.TestData;
using Manx_Search_Data.TestUtil;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace Manx_Search_Data
{
    [TestFixture]
    public class DocumentTests
    {
        [DatapointSource]
        // ReSharper disable once UnusedMember.Global
        public Document[] AllDocuments = Documents.AllDocuments.ToArray();

        [Theory]
        public void DefinitionHasName(Document definition)
        {
            Assert.That(definition.Name, Is.Not.Null, $"{nameof(Document.Name)} should be defined");
            Assert.That(definition.Name, Is.Not.Empty, $"{nameof(Document.Name)} should be non-empty");
        }        
        
        [Theory]
        public void DefinitionHasIdent(Document definition)
        {
            Assert.That(definition.Ident, Is.Not.Null, $"{nameof(Document.Ident)} should be defined. This is the how the document is defined in a web address");
            Assert.That(definition.Ident, Is.Not.Empty, $"{nameof(Document.Ident)} should be non-empty. This is the how the document is defined in a web address");
            Assert.That(definition.Ident, Does.Match("^[A-Za-z0-9\\-]+$"), $"{nameof(Document.Ident)} must only contain letters numbers, or dashes with no spaces.\nThis field is the how the document is defined in a web address");
        }

        [Theory]
        public void DefinitionHasDate(Document definition)
        {
            Assert.That(definition.CreatedCircaStart, Is.Not.Null, $"Either '{nameof(Document.Created)}' or '{nameof(Document.CreatedCircaStart)}' must be set");
            Assert.That(definition.CreatedCircaEnd, Is.Not.Null, $"Either '{nameof(Document.Created)}' or '{nameof(Document.CreatedCircaEnd)}' must be set");
        }

        [Theory]
        public void DefinitionDatesAreValid(Document definition)
        {
            Assert.That(definition.CreatedCircaStart, Is.LessThanOrEqualTo(definition.CreatedCircaEnd), $"'{nameof(Document.CreatedCircaStart)}' was greater than '{nameof(Document.CreatedCircaEnd)}'");
        }

        [Theory]
        public void DefinitionsHasLinkedCsv(Document definition)
        {
            Assert.That(definition.CsvFileName, Is.Not.Null, $"'{nameof(Document.CsvFileName)}' must be set");
        }

        [Theory]
        public void PdfLinkIsValidIfDefined(Document definition)
        {
            var document = AssumeOpenSource(definition, "PDF is not available yet");

            Assert.That(document.PdfFileName, Is.Null.Or.Not.Empty, $"'{nameof(Document.PdfFileName)}' must not have an empty value (either delete the line, or set a value)");

            if (document.PdfFileName == null)
            {
                return;
            }

            Assert.That(File.Exists(document.FullPdfPath), $"'{document.FullPdfPath}' does not exist");
        }

        [Theory]
        public void CsvFileIsReadable(Document definition)
        {
            AssumeOpenSource(definition, "CSV is not available yet");

            try
            {
                var lines = definition.LoadLocalFile();
                Assert.That(lines, Is.Not.Empty, "No data detected in CSV");
            }
            catch (Exception e)
            {
                Assert.Fail($"{definition} has an invalid CSV: {e}");
            }
        }

        [Theory]
        public void CsvFileIsUtf8(Document document)
        {
            var openSourceDocument = AssumeOpenSource(document, "CSV is not available yet");
            var lines = document.LoadLocalFile();

            // We perform this per-line as we don't want the entire text in the unit test output.
            foreach (string line in lines.Select(x => x.English + "|" + x.Manx))
            {
                // If we don't use Ordinal, then CI fails at index 0 (even though the value is ASCII 65 (A)).
                int index = line.IndexOf("�", StringComparison.Ordinal);
                if (index == -1)
                {
                    continue;
                }

                Assert.That(line, Does.Not.Contain("�"), $"The CSV is saved incorrectly. Please open it in Notepad++ and select \"Encoding - Convert to UTF-8\"\nFile: {openSourceDocument.FullCsvPath}. Index: {index}");
            }
        }

        [Theory]
        public void CsvFileIsNotChinese(Document document)
        {
            // PERF: this is a slow test - combine with CsvFileIsUTF8
            // Issue: 463 - some files were interpreted to be Chinese, the only way to fix this was to re-save in Excel as UTF-8
            var openSourceDocument = AssumeOpenSource(document, "CSV is not available yet");
            var lines = document.LoadLocalFile();

            const string chineseOrJapanese = "[\u3040-\u30ff\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff\uff66-\uff9f]";

            foreach (string line in lines.Select(x => x.English + "|" + x.Manx))
            {
                Assert.That(line, Does.Not.Match(chineseOrJapanese), $"The CSV may be saved incorrectly (containing Chinese/Japanese Text). Please remake it and save it as CSV (UTF-8) in Excel. Contact David if this fails\"\nFile: {openSourceDocument.FullCsvPath}.");
            }
        }

        [Theory]
        public void LicenseExists(Document definition)
        {
            var openSourceDocument = AssumeOpenSource(definition, "license is not available yet");

            Assert.That(File.Exists(openSourceDocument.LicenseLink), $"{nameof(OpenSourceDocument.LicenseLink)} does not exist");
        }

        [Theory]
        public void DocumentsWithoutOriginalIsValid(Document document)
        {
            var openSourceDocument = AssumeOpenSource(document,  "'original' is not available yet");
            
            // If we add an 'original', it should be removed from the list,
            // this stops me wasting time by requesting an original more than once
            if (!DocumentsWithoutOriginal.DocsWithoutOriginal.Contains(openSourceDocument.Ident))
            {
                return;
            } 
            
            Assert.That(openSourceDocument.Original, Is.Null, openSourceDocument.Ident);
        }

        [Theory]
        public void OriginalIsDefined(Document document)
        {
            var openSourceDocument = AssumeOpenSource(document,  "'original' is not available yet");

            Assume.That(openSourceDocument.Ident, Is.Not.AnyOf(DocumentsWithoutOriginal.DocsWithoutOriginal),  document.Ident + " has no 'Original' defined");
            
            Assert.That(openSourceDocument.Original,Is.AnyOf("Manx", "English", "Unknown", "Bilingual", "Neither"));
        }
        
        [Theory]
        public void ValidateOriginalDocumentColumn(Document document)
        {
            var openSourceDocument = AssumeOpenSource(document,  "'original' is not available yet");

            var headers = openSourceDocument.LoadHeaders();

            var invalidHeaders = new[] { "Original Manx", "Original English", "Manx Orginal", "English Orginal" }.ToHashSet();

            foreach (var header in invalidHeaders)
            {
                Assert.That(headers, Does.Not.Contain(header));
            }
        }
        
        [Theory]
        public void ColumnsWithDataMustHaveATitle(Document document)
        {
            // we have a number of documents where the 'Notes' header was not provided
            var openSourceDocument = AssumeOpenSource(document,  "'original' is not available yet");

            var headers = openSourceDocument.LoadHeaders();
            
            foreach (var i in headers.Select((h,i) => (header: h,index: i)).Where(x => x.header == string.Empty).Select(x => x.index))
            {
                // assert all cells are empty
                // no need to give the index of either - we know the file and it's obvious
                var invalidCells = openSourceDocument.LoadColumn(i).Select(x => x).Where(x => x != "");
                Assert.That(invalidCells, Is.Empty);
            }
        }
        
                
        [Theory]
        public void NoTypos(Document document)
        {
            var openSourceDocument = AssumeOpenSource(document,  "'original' is not available yet");

            // ReSharper disable StringLiteralTypo
            Assert.That(openSourceDocument.Ident, Does.Not.Contain("coraa-ny-gaal").IgnoreCase, "Should be 'coraa-ny-gael'");
            Assert.That(openSourceDocument.Name, Does.Not.Contain("Slattysn").IgnoreCase, "Should be 'Slattysyn'");
            Assert.That(openSourceDocument.Ident, Does.Not.Contain("Slattysn").IgnoreCase, "Should be 'Slattysyn'");
            // ReSharper restore StringLiteralTypo
        }

        [Theory]
        public void NoCopies(Document document)
        {
            var openSourceDocument = AssumeOpenSource(document,  "'original' is not available yet");

            Assert.That(openSourceDocument.FullCsvPath, Does.Not.EndWith(" copy/document.csv"));
        }

        /// <summary>We currently have files which are not yet licensed for usage on GitHub, some checks cannot be run on these yet</summary>
        private static OpenSourceDocument AssumeOpenSource(Document definition, string reasonAsClosedSource)
        {
            if (ClosedSourceDocuments.documents.Contains(definition))
            {
                // We don't use "Assume" here as we want clean test output
                // TODO: Figure out getting dotnet test to list clean output
                Assert.Pass($"Skipping - document was closed source - {reasonAsClosedSource}");
            }

            return (OpenSourceDocument)definition;
        }
    }
}
