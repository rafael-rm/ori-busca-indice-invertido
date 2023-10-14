using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using StopWord;

namespace IndiceInvertido.Util;

public class InvertedIndex
{
    private Dictionary<string, List<string>>? _invertedIndex;

    public Task GenerateInvertedIndex(Dictionary<string, string> documents)
    {
        var invertedIndex = new Dictionary<string, List<string>>();

        foreach (var document in documents)
        {
            string[] words = document.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                if (!invertedIndex.ContainsKey(word))
                    invertedIndex[word] = new List<string>();

                invertedIndex[word].Add(document.Key);
            }
        }

        _invertedIndex = invertedIndex;
        return Task.CompletedTask;
    }

    public async Task StartIndex()
    {
        if (_invertedIndex is null)
        {
            string? projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.FullName;
        
            string pathFolderHtmls = projectDirectory + "/IndiceInvertido/Data/htmlFiles";
            
            Dictionary<string, string> textTreated = await GetTreatedTextFiles(pathFolderHtmls);
            
            await GenerateInvertedIndex(textTreated);
        }
    }

    public async Task<List<string>> Search(string query)
    {
        await StartIndex();
        
        string[] terms = query.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> result = new List<string>();

        foreach (string term in terms)
        {
            if (term.Equals("and", StringComparison.OrdinalIgnoreCase))
            {
                result = Intersect(result, await Search(terms[Array.IndexOf(terms, term) - 1]));
            }
            else if (term.Equals("or", StringComparison.OrdinalIgnoreCase))
            {
                result = Union(result, await Search(terms[Array.IndexOf(terms, term) - 1]));
            }
            else if (term.Equals("not", StringComparison.OrdinalIgnoreCase))
            {
                result = Difference(result, await Search(terms[Array.IndexOf(terms, term) + 1]));
            }
            else
            {
                List<string> termResults;
                if (_invertedIndex.TryGetValue(term, out termResults))
                {
                    result = result.Count == 0 ? termResults : Intersect(result, termResults);
                }
                else
                {
                    result = new List<string>();
                }
            }
        }

        return result;
    }

    private List<string> Intersect(List<string> list1, List<string> list2)
    {
        return list1.Intersect(list2).ToList();
    }

    private List<string> Union(List<string> list1, List<string> list2)
    {
        return list1.Union(list2).ToList();
    }

    private List<string> Difference(List<string> list1, List<string> list2)
    {
        return list1.Except(list2).ToList();
    }
    
    public static async Task<Dictionary<string, string>> GetPlainTextFiles(string pathFolder)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string[] filesHtml = Directory.GetFiles(pathFolder, "*.htm*");

        Dictionary<string, string> filesText = new Dictionary<string, string>();
        foreach (string file in filesHtml)
        {
            string fileName = Path.GetFileName(file);
            string fileText = await File.ReadAllTextAsync(file, Encoding.GetEncoding(1252));
            filesText.TryAdd(fileName, fileText);
            
            Console.WriteLine($"[INFO] Arquivo {fileName} carregado com sucesso.");
        }
        
        Console.WriteLine($"[INFO] Total de arquivos carregados: {filesText.Count}");

        return filesText;
    }
    
    public static async Task<Dictionary<string, string>> GetTreatedTextFiles(string filePath)
    {
        Dictionary<string, string> dictionaryFiles = await GetPlainTextFiles(filePath);

        foreach (KeyValuePair<string, string> file in dictionaryFiles)
        {
            string text = file.Value;
            text = Regex.Replace(text, "<.*?>", string.Empty);
            text = WebUtility.HtmlDecode(text);
            text = text.Replace("-", " ");
            text = Regex.Replace(text, @"\s+", " ");
            text = text.ToLower();
            text = Regex.Replace(text, @"[:;()\[\].,?!<>]", string.Empty);
            text = Regex.Replace(text, @"[^\p{L}\d\sÀ-ÖØ-öø-ÿ]", string.Empty);
            text = text.Normalize(NormalizationForm.FormD);
            text = new string(text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
            
            Console.WriteLine($"[INFO] Texto do arquivo {file.Key} limpo e normalizado com sucesso.");
            
            text = text.RemoveStopWords("pt");
            text = text.RemoveStopWords("en");
            
            Console.WriteLine($"[INFO] Removido stop-words do texto do arquivo {file.Key} com sucesso.");
            
            dictionaryFiles[file.Key] = text;
        }

        Console.WriteLine($"[INFO] Todos os textos dos {dictionaryFiles.Count} foram limpos e normalizados com sucesso.");
        
        return dictionaryFiles;
    }

    public static Task<Dictionary<string, int>> GetRepetitionCount(string text)
    {
        Dictionary<string, int> wordCount = new Dictionary<string, int>();
        
        Console.WriteLine("[INFO] Iniciando contagem das repetições de palavras.");

        foreach (string word in text.Split(' '))
        {
            if (string.IsNullOrEmpty(word))
                continue;

            if (wordCount.ContainsKey(word))
            {
                wordCount[word]++;
                Console.WriteLine($"[INFO] Palavra {word} somada no dicionario, contagem atual: {wordCount[word]}");
            }
            else
            {
                wordCount[word] = 1;
                Console.WriteLine($"[INFO] Palavra {word} adicionada ao dicionario, contagem atual: {wordCount[word]}");
            }
        }
        
        Console.WriteLine("[INFO] Contagem de repetições de palavras finalizada com sucesso.");

        return Task.FromResult(wordCount);
    }

    public static Task<Dictionary<string, int>> OrderRepetition(Dictionary<string, int> dictionary, bool descending = true)
    {
        Dictionary<string, int> orderedDictionary;

        orderedDictionary = descending 
            ? dictionary.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value) 
            : dictionary.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

        Console.WriteLine("[INFO] Dicionario ordenado com sucesso.");
        
        return Task.FromResult(orderedDictionary);
    }

    public static Task<string> GetAllTextDictonary(Dictionary<string, string> dictionary)
    {
        string allText = string.Empty;
        
        foreach (KeyValuePair<string, string> file in dictionary)
        {
            allText += file.Value;
        }
        
        Console.WriteLine($"$[INFO] O texto de {dictionary.Count} arquivos foi mesclado com sucesso.");
        Console.WriteLine($"$[INFO] Caracteres totais: {allText.Length}");
        
        return Task.FromResult(allText);
    }
}