<Query Kind="Statements" />

// order: 7
var edges = "0 1 10\n0 6 15\n1 2 15\n2 3 12\n6 4 30\n0 2 9\n3 4 16\n4 5 9\n5 0 17\n0 2 8\n1 3 21\n5 6 94\n2 4 14\n3 5 13\n6 4 50\n4 0 20\n5 1 7\n6 3 68\n5 5 1\n";
// source: 0

foreach (var toks in edges.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                          .Select(line => line.Split(' '))) {
    Console.WriteLine($".Edge({toks[0]}, {toks[1]}, {toks[2]})");
}
