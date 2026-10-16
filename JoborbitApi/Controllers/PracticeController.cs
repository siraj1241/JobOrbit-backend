using JobsApi.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoborbitApi.Controllers;

/// <summary>
/// Practice endpoint — coding puzzles for SQL, algorithms, and MCQs.
/// All questions are hardcoded (no DB needed) so the feature works even
/// before the database is connected.
/// </summary>
[ApiController]
[Route("api/practice")]
[AllowAnonymous]
public class PracticeController : ControllerBase
{
    // ─── Sample data ───────────────────────────────────────────────

    private static readonly SqlPuzzleDto[] SqlPuzzles = new[]
    {
        new SqlPuzzleDto(
            Id: "sql-1",
            Title: "Find the highest-paid employee",
            Difficulty: "Easy",
            Description: "You have a table `employees` with columns: id, name, department, salary. Write a SQL query to find the employee with the highest salary.",
            Schema: "CREATE TABLE employees (\n  id INT PRIMARY KEY,\n  name VARCHAR(100),\n  department VARCHAR(50),\n  salary DECIMAL(10, 2)\n);\n\nINSERT INTO employees VALUES\n  (1, 'Alice', 'Engineering', 95000),\n  (2, 'Bob', 'Sales', 72000),\n  (3, 'Carol', 'Engineering', 110000),\n  (4, 'Dave', 'Marketing', 68000);",
            Hints: new[] { "Use ORDER BY", "Use LIMIT 1", "Sort by salary descending" },
            ExpectedKeywords: new[] { "ORDER BY", "salary", "DESC", "LIMIT" },
            SampleSolution: "SELECT * FROM employees ORDER BY salary DESC LIMIT 1;"
        ),
        new SqlPuzzleDto(
            Id: "sql-2",
            Title: "Count employees per department",
            Difficulty: "Easy",
            Description: "Write a SQL query that returns each department and the number of employees in it, sorted by count descending.",
            Schema: "CREATE TABLE employees (\n  id INT PRIMARY KEY,\n  name VARCHAR(100),\n  department VARCHAR(50),\n  salary DECIMAL(10, 2)\n);",
            Hints: new[] { "Use GROUP BY", "Use COUNT(*)", "Don't forget ORDER BY" },
            ExpectedKeywords: new[] { "GROUP BY", "department", "COUNT", "ORDER BY" },
            SampleSolution: "SELECT department, COUNT(*) FROM employees GROUP BY department ORDER BY COUNT(*) DESC;"
        ),
        new SqlPuzzleDto(
            Id: "sql-3",
            Title: "Second highest salary",
            Difficulty: "Medium",
            Description: "Find the second-highest salary in the employees table. If there's only one distinct salary, return NULL.",
            Schema: "CREATE TABLE employees (\n  id INT PRIMARY KEY,\n  salary DECIMAL(10, 2)\n);",
            Hints: new[] { "Use DISTINCT", "Use LIMIT with OFFSET", "Or use a subquery with MAX" },
            ExpectedKeywords: new[] { "SELECT", "DISTINCT", "salary", "LIMIT", "OFFSET" },
            SampleSolution: "SELECT DISTINCT salary FROM employees ORDER BY salary DESC LIMIT 1 OFFSET 1;"
        ),
    };

    private static readonly AlgorithmPuzzleDto[] AlgoPuzzles = new[]
    {
        new AlgorithmPuzzleDto(
            Id: "algo-1",
            Title: "Two Sum",
            Difficulty: "Easy",
            Description: "Given an array of integers and a target, return the indices of the two numbers that add up to the target. You may assume exactly one solution exists.",
            Signature: "function twoSum(nums, target) {\n  // your code here\n}",
            Hints: new[] { "Use a hash map to store seen numbers", "Look up complement = target - num", "Single pass through the array = O(n)" },
            OptimalComplexity: "O(n)",
            SampleSolution: "function twoSum(nums, target) {\n  const seen = {};\n  for (let i = 0; i < nums.length; i++) {\n    const complement = target - nums[i];\n    if (seen[complement] !== undefined) return [seen[complement], i];\n    seen[nums[i]] = i;\n  }\n}"
        ),
        new AlgorithmPuzzleDto(
            Id: "algo-2",
            Title: "Reverse a string",
            Difficulty: "Easy",
            Description: "Reverse a string in place. Don't use the built-in reverse() method.",
            Signature: "function reverseString(s) {\n  // your code here\n}",
            Hints: new[] { "Use two pointers (start, end)", "Swap characters", "Move pointers inward" },
            OptimalComplexity: "O(n)",
            SampleSolution: "function reverseString(s) {\n  let arr = s.split('');\n  let left = 0, right = arr.length - 1;\n  while (left < right) {\n    [arr[left], arr[right]] = [arr[right], arr[left]];\n    left++; right--;\n  }\n  return arr.join('');\n}"
        ),
        new AlgorithmPuzzleDto(
            Id: "algo-3",
            Title: "Binary search",
            Difficulty: "Medium",
            Description: "Given a sorted array and a target value, return the index of the target if found, else -1. Must run in O(log n) time.",
            Signature: "function binarySearch(arr, target) {\n  // your code here\n}",
            Hints: new[] { "Use two pointers: low, high", "Compute mid = (low + high) / 2", "Halve the search space each iteration" },
            OptimalComplexity: "O(log n)",
            SampleSolution: "function binarySearch(arr, target) {\n  let low = 0, high = arr.length - 1;\n  while (low <= high) {\n    const mid = Math.floor((low + high) / 2);\n    if (arr[mid] === target) return mid;\n    arr[mid] < target ? low = mid + 1 : high = mid - 1;\n  }\n  return -1;\n}"
        ),
        new AlgorithmPuzzleDto(
            Id: "algo-4",
            Title: "Fibonacci (memoized)",
            Difficulty: "Medium",
            Description: "Return the n-th Fibonacci number. Use memoization to avoid exponential time. F(0)=0, F(1)=1, F(n)=F(n-1)+F(n-2).",
            Signature: "function fib(n, memo = {}) {\n  // your code here\n}",
            Hints: new[] { "Store computed results in memo object", "Base cases: F(0)=0, F(1)=1", "Check memo before computing" },
            OptimalComplexity: "O(n)",
            SampleSolution: "function fib(n, memo = {}) {\n  if (n < 2) return n;\n  if (memo[n]) return memo[n];\n  return memo[n] = fib(n - 1, memo) + fib(n - 2, memo);\n}"
        ),
    };

    private static readonly McqDto[] Mcqs = new[]
    {
        new McqDto(
            Id: "mcq-1",
            Question: "What is the time complexity of binary search on a sorted array of size n?",
            Options: new[] { "O(1)", "O(log n)", "O(n)", "O(n log n)" },
            CorrectIndex: 1,
            Explanation: "Binary search halves the search space on each iteration, giving O(log n) time complexity."
        ),
        new McqDto(
            Id: "mcq-2",
            Question: "Which data structure uses FIFO (First-In-First-Out) ordering?",
            Options: new[] { "Stack", "Queue", "Binary Tree", "Hash Map" },
            CorrectIndex: 1,
            Explanation: "A queue processes elements in FIFO order — the first element added is the first one removed."
        ),
        new McqDto(
            Id: "mcq-3",
            Question: "In SQL, which clause is used to filter rows AFTER a GROUP BY operation?",
            Options: new[] { "WHERE", "HAVING", "FILTER", "AFTER" },
            CorrectIndex: 1,
            Explanation: "HAVING filters rows after GROUP BY aggregates them. WHERE filters rows before grouping."
        ),
        new McqDto(
            Id: "mcq-4",
            Question: "What is the worst-case time complexity of inserting into a hash map?",
            Options: new[] { "O(1)", "O(log n)", "O(n)", "O(n^2)" },
            CorrectIndex: 2,
            Explanation: "Average case is O(1), but worst case (all keys collide to the same bucket) is O(n)."
        ),
        new McqDto(
            Id: "mcq-5",
            Question: "Which HTTP status code indicates 'I'm a teapot' (an April Fools' joke from RFC 2324)?",
            Options: new[] { "404", "418", "500", "302" },
            CorrectIndex: 1,
            Explanation: "HTTP 418 'I'm a teapot' was defined in RFC 2324 (HTCPCP). Reserved for jokes; never used in real APIs."
        ),
        new McqDto(
            Id: "mcq-6",
            Question: "In React, which hook should you use for side effects (e.g. fetching data)?",
            Options: new[] { "useState", "useMemo", "useEffect", "useRef" },
            CorrectIndex: 2,
            Explanation: "useEffect runs side effects after render. Common uses: data fetching, subscriptions, manual DOM updates."
        ),
        new McqDto(
            Id: "mcq-7",
            Question: "What does ACID stand for in database transactions?",
            Options: new[] {
                "Atomicity, Consistency, Isolation, Durability",
                "Async, Cache, Index, Data",
                "Atomicity, Concurrency, Indexing, Distributed",
                "Access, Consistency, Integrity, Data"
            },
            CorrectIndex: 0,
            Explanation: "ACID = Atomicity (all or nothing), Consistency (valid state), Isolation (concurrent txns don't interfere), Durability (committed = permanent)."
        ),
        new McqDto(
            Id: "mcq-8",
            Question: "What is the time complexity of accessing an element in an array by index?",
            Options: new[] { "O(1)", "O(log n)", "O(n)", "O(n^2)" },
            CorrectIndex: 0,
            Explanation: "Array access by index is O(1) — constant time — because the memory address is computed directly from the index."
        ),
    };

    // ─── Endpoints ────────────────────────────────────────────────

    /// <summary>List all SQL puzzles.</summary>
    [HttpGet("sql")]
    public IActionResult ListSqlPuzzles() => Ok(SqlPuzzles);

    /// <summary>List all algorithm puzzles.</summary>
    [HttpGet("algorithms")]
    public IActionResult ListAlgorithms() => Ok(AlgoPuzzles);

    /// <summary>List all multiple-choice questions.</summary>
    [HttpGet("mcq")]
    public IActionResult ListMcqs() => Ok(Mcqs);

    /// <summary>Check a SQL solution by matching expected keywords (case-insensitive).</summary>
    [HttpPost("sql/{id}/check")]
    public IActionResult CheckSql(string id, [FromBody] CheckSolutionRequest req)
    {
        var puzzle = SqlPuzzles.FirstOrDefault(p => p.Id == id)
            ?? throw new NotFoundException("SqlPuzzle", id);

        var code = (req.Code ?? "").ToUpperInvariant();
        var found = puzzle.ExpectedKeywords
            .Where(k => code.Contains(k.ToUpperInvariant()))
            .ToList();
        var missing = puzzle.ExpectedKeywords.Except(found).ToList();
        var passed = missing.Count == 0 && code.Length > 10;

        return Ok(new CheckResultDto(
            Passed: passed,
            Score: found.Count,
            Total: puzzle.ExpectedKeywords.Length,
            FoundKeywords: found,
            MissingKeywords: missing,
            Feedback: passed
                ? "✅ All required SQL keywords detected. Looks correct!"
                : $"⚠ Missing keywords: {string.Join(", ", missing)}. Review the hints.",
            SampleSolution: passed ? null : puzzle.SampleSolution
        ));
    }

    /// <summary>Analyze the time complexity of a code snippet (pattern-based, heuristic).</summary>
    [HttpPost("algorithms/{id}/analyze")]
    public IActionResult AnalyzeAlgorithm(string id, [FromBody] CheckSolutionRequest req)
    {
        var puzzle = AlgoPuzzles.FirstOrDefault(p => p.Id == id)
            ?? throw new NotFoundException("AlgorithmPuzzle", id);

        var code = req.Code ?? "";
        var (complexity, reasons) = AnalyzeComplexity(code);

        var isOptimal = complexity == puzzle.OptimalComplexity;

        return Ok(new ComplexityResultDto(
            DetectedComplexity: complexity,
            OptimalComplexity: puzzle.OptimalComplexity,
            IsOptimal: isOptimal,
            Reasons: reasons,
            Feedback: isOptimal
                ? "✅ Optimal time complexity! Your solution scales well."
                : $"⚠ Your solution is {complexity} but the optimal is {puzzle.OptimalComplexity}. See hints to improve.",
            SampleSolution: isOptimal ? null : puzzle.SampleSolution
        ));
    }

    /// <summary>Check an MCQ answer.</summary>
    [HttpPost("mcq/{id}/check")]
    public IActionResult CheckMcq(string id, [FromBody] CheckMcqRequest req)
    {
        var q = Mcqs.FirstOrDefault(m => m.Id == id)
            ?? throw new NotFoundException("Mcq", id);

        var correct = req.SelectedIndex == q.CorrectIndex;
        return Ok(new McqResultDto(
            Correct: correct,
            SelectedIndex: req.SelectedIndex,
            CorrectIndex: q.CorrectIndex,
            Explanation: q.Explanation
        ));
    }

    // ─── Big-O analyzer (heuristic, pattern-based) ────────────────

    private static (string complexity, List<string> reasons) AnalyzeComplexity(string code)
    {
        var reasons = new List<string>();
        var hasNestedLoop = CountOccurrences(code, "for") >= 2 || CountOccurrences(code, "while") >= 2;
        var hasSingleLoop = CountOccurrences(code, "for") >= 1 || CountOccurrences(code, "while") >= 1;
        var hasSortCall = code.Contains(".sort(") || code.Contains("sort(");
        var hasHashMap = code.Contains("Map(") || code.Contains("{}") || code.Contains("Set(") || code.Contains("memo");
        var hasRecursion = HasRecursion(code);
        var halvesEachStep = code.Contains("/ 2") || code.Contains("/2") || code.Contains("Math.floor((low + high)");

        // Heuristic priority: most expensive first
        if (hasNestedLoop && !hasHashMap)
        {
            reasons.Add("Nested loops detected — each iteration of the outer loop runs the inner loop fully.");
            return ("O(n^2)", reasons);
        }

        if (hasSortCall)
        {
            reasons.Add("Built-in sort() call — typically O(n log n) in modern engines.");
            return ("O(n log n)", reasons);
        }

        if (hasSingleLoop && halvesEachStep)
        {
            reasons.Add("Loop that halves the search space each iteration → logarithmic time.");
            return ("O(log n)", reasons);
        }

        if (hasRecursion && !hasHashMap)
        {
            reasons.Add("Unmemoized recursion → exponential time (typically O(2^n) for problems like naive Fibonacci).");
            return ("O(2^n)", reasons);
        }

        if (hasSingleLoop)
        {
            reasons.Add("Single loop iterating over the input → linear time.");
            return ("O(n)", reasons);
        }

        if (hasHashMap)
        {
            reasons.Add("Hash map / memoization used → constant-time lookups.");
            return ("O(n)", reasons);
        }

        reasons.Add("No loops or recursion detected — constant time operations only.");
        return ("O(1)", reasons);
    }

    private static int CountOccurrences(string source, string substring)
    {
        var count = 0;
        var i = 0;
        while ((i = source.IndexOf(substring, i, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            i += substring.Length;
        }
        return count;
    }

    private static bool HasRecursion(string code)
    {
        // Naive detection: function name appears inside its own body
        var match = System.Text.RegularExpressions.Regex.Match(
            code, @"function\s+(\w+)\s*\(", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success) return false;
        var fnName = match.Groups[1].Value;
        return code.IndexOf(fnName, match.Index + match.Length) != -1;
    }

    // ─── DTOs ────────────────────────────────────────────────────

    public record CheckSolutionRequest(string Code);
    public record CheckMcqRequest(int SelectedIndex);

    public record SqlPuzzleDto(
        string Id, string Title, string Difficulty, string Description,
        string Schema, string[] Hints, string[] ExpectedKeywords, string SampleSolution);

    public record AlgorithmPuzzleDto(
        string Id, string Title, string Difficulty, string Description,
        string Signature, string[] Hints, string OptimalComplexity, string SampleSolution);

    public record McqDto(
        string Id, string Question, string[] Options, int CorrectIndex, string Explanation);

    public record CheckResultDto(
        bool Passed, int Score, int Total,
        IReadOnlyList<string> FoundKeywords, IReadOnlyList<string> MissingKeywords,
        string Feedback, string? SampleSolution);

    public record ComplexityResultDto(
        string DetectedComplexity, string OptimalComplexity, bool IsOptimal,
        IReadOnlyList<string> Reasons, string Feedback, string? SampleSolution);

    public record McqResultDto(
        bool Correct, int SelectedIndex, int CorrectIndex, string Explanation);
}
