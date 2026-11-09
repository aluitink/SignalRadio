using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SignalRadio.Core.Interfaces;

namespace SignalRadio.Api.Services;

/// <summary>
/// Local CPU embedding client using ONNX Runtime + all-MiniLM-L6-v2 (384 dims).
/// Model files (model.onnx, tokenizer.json) are loaded from AI:ModelPath.
/// Uses mean-pooling + L2 normalization (sentence-transformers convention).
/// </summary>
public class LocalEmbeddingClient : IAiClient
{
    private readonly InferenceSession _session;
    private readonly TokenizerData _tokenizer;
    private readonly ILogger<LocalEmbeddingClient> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsEnabled => true;
    public bool AreEmbeddingsEnabled => true;

    public LocalEmbeddingClient(IConfiguration config, ILogger<LocalEmbeddingClient> logger)
    {
        _logger = logger;
        var modelPath = config["AI:ModelPath"]
            ?? throw new InvalidOperationException("AI:ModelPath is required for local embeddings.");

        var modelFile = Path.Combine(modelPath, "model.onnx");
        var tokenizerFile = Path.Combine(modelPath, "tokenizer.json");

        if (!File.Exists(modelFile))
            throw new FileNotFoundException($"ONNX model not found at {modelFile}");
        if (!File.Exists(tokenizerFile))
            throw new FileNotFoundException($"Tokenizer not found at {tokenizerFile}");

        _session = new InferenceSession(modelFile);
        _tokenizer = TokenizerData.Load(tokenizerFile);
        _logger.LogInformation("Local embedding model loaded from {Path} (384 dims).", modelPath);
    }

    public Task<string> SummarizeAsync(string text, CancellationToken ct = default) =>
        Task.FromResult(text.Length > 200 ? text[..200] + "..." : text);

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var (inputIds, attentionMask) = _tokenizer.Encode(text, maxTokens: 128);
            var n = inputIds.Length;
            if (n == 0) return new float[384];

            var tokenTypeIds = new long[n]; // all zeros for MiniLM

            // Model expects 2D inputs: [batch=1, seq_len=n]
            var ids2d = new long[n]; Array.Copy(inputIds, ids2d, n);
            var mask2d = new long[n]; Array.Copy(attentionMask, mask2d, n);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(ids2d.AsMemory(), [1, n], false)),
                NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(mask2d.AsMemory(), [1, n], false)),
                NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(tokenTypeIds.AsMemory(), [1, n], false))
            };

            using var results = _session.Run(inputs);

            // Output: last_hidden_state [1, seq_len, 384]
            var hidden = results.First(r => r.Name == "last_hidden_state").AsTensor<float>();
            var dim = hidden.Dimensions[2];

            // Mean pooling over non-padded tokens
            var embedding = new float[dim];
            var count = 0f;
            for (var i = 0; i < n; i++)
            {
                if (attentionMask[i] == 0) continue;
                count += 1f;
                for (var d = 0; d < dim; d++)
                    embedding[d] += hidden[new int[] { 0, i, d }];
            }

            if (count > 0)
                for (var d = 0; d < dim; d++)
                    embedding[d] /= count;

            // L2 normalize
            var norm = (float)Math.Sqrt(embedding.Sum(v => v * v));
            if (norm > 0)
                for (var d = 0; d < dim; d++)
                    embedding[d] /= norm;

            return embedding;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// BERT WordPiece tokenizer loaded from tokenizer.json.
    /// Handles [CLS]/[SEP], max length, and WordPiece subword decomposition.
    /// </summary>
    private sealed class TokenizerData
    {
        private readonly Dictionary<string, int> _vocab;
        private readonly int _clsId;
        private readonly int _sepId;
        private readonly int _unkId;
        private readonly int _maxWordLen;
        private readonly string _subwordPrefix;

        private TokenizerData(
            Dictionary<string, int> vocab,
            int clsId, int sepId, int unkId, int maxWordLen, string subwordPrefix)
        {
            _vocab = vocab;
            _clsId = clsId;
            _sepId = sepId;
            _unkId = unkId;
            _maxWordLen = maxWordLen;
            _subwordPrefix = subwordPrefix;
        }

        public static TokenizerData Load(string path)
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var vocab = new Dictionary<string, int>(StringComparer.Ordinal);

            if (root.TryGetProperty("model", out var model))
            {
                if (model.TryGetProperty("vocab", out var vocabObj))
                {
                    foreach (var prop in vocabObj.EnumerateObject())
                        vocab[prop.Name] = prop.Value.GetInt32();
                }

                int maxWordLen = 20;
                if (model.TryGetProperty("max_input_chars_per_word", out var mwl))
                    maxWordLen = mwl.GetInt32();

                string subwordPrefix = "##";
                if (model.TryGetProperty("continuing_subword_prefix", out var prefix))
                    subwordPrefix = prefix.GetString() ?? "##";

                int clsId = 101, sepId = 102, unkId = 100;
                if (root.TryGetProperty("added_tokens", out var added))
                {
                    foreach (var token in added.EnumerateArray())
                    {
                        var content = token.GetProperty("content").GetString() ?? "";
                        var id = token.GetProperty("id").GetInt32();
                        if (content == "[CLS]") clsId = id;
                        else if (content == "[SEP]") sepId = id;
                        else if (content == "[UNK]") unkId = id;
                    }
                }

                return new TokenizerData(vocab, clsId, sepId, unkId, maxWordLen, subwordPrefix);
            }

            throw new InvalidDataException($"tokenizer.json at {path} does not contain a model section.");
        }

        public (long[] inputIds, long[] attentionMask) Encode(string text, int maxTokens)
        {
            var tokens = new List<int>(maxTokens);
            tokens.Add(_clsId);

            var normalized = PreTokenize(text);
            foreach (var word in normalized)
            {
                if (tokens.Count >= maxTokens - 1) break;
                tokens.AddRange(WordPieceEncode(word, maxTokens - tokens.Count));
            }

            tokens.Add(_sepId);

            var n = tokens.Count;
            var inputIds = new long[n];
            var attentionMask = new long[n];
            for (var i = 0; i < n; i++)
            {
                inputIds[i] = tokens[i];
                attentionMask[i] = 1;
            }

            return (inputIds, attentionMask);
        }

        private static List<string> PreTokenize(string text)
        {
            // BertPreTokenizer: split on non-alphanumeric characters,
            // keeping each symbol as its own token.
            var result = new List<string>();
            var lower = text.ToLowerInvariant();
            var current = new System.Text.StringBuilder();

            foreach (var c in lower)
            {
                if (char.IsLetterOrDigit(c))
                {
                    current.Append(c);
                }
                else
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                    if (!char.IsWhiteSpace(c))
                        result.Add(c.ToString());
                }
            }

            if (current.Length > 0)
                result.Add(current.ToString());

            return result;
        }

        private List<int> WordPieceEncode(string word, int maxSubTokens)
        {
            if (word.Length > _maxWordLen)
                return [_unkId];

            // Greedy WordPiece: longest-prefix matching with ## continuation.
            var ids = new List<int>();
            var start = 0;

            while (start < word.Length)
            {
                var end = word.Length;
                int foundId = -1;

                while (end > start)
                {
                    var piece = word[start..end];
                    var key = start == 0 ? piece : _subwordPrefix + piece;
                    if (_vocab.TryGetValue(key, out var id))
                    {
                        foundId = id;
                        break;
                    }
                    end--;
                }

                if (foundId < 0)
                {
                    // No subword found from this position — emit [UNK] and advance one char.
                    ids.Add(_unkId);
                    start++;
                }
                else
                {
                    ids.Add(foundId);
                    start = end;
                }

                if (ids.Count >= maxSubTokens)
                    break;
            }

            return ids;
        }
    }
}
