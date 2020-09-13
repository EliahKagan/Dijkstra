<Query Kind="Program" />

#load "./dijkstra.linq"

private static void Main()
{
    IPriorityQueue<string, int> pq = new FibonacciHeap<string, int>();
    pq.InsertOrDecrease("bar", 48);
    pq.InsertOrDecrease("foo", 23);
    pq.InsertOrDecrease("baz", 90);
    pq.Count.Dump();
    pq.ExtractMin().Dump();
    pq.Count.Dump();
    pq.InsertOrDecrease("quux", 56);
    pq.Count.Dump();
    pq.ExtractMin().Dump();
    pq.Count.Dump();
    pq.ExtractMin().Dump();
    pq.Count.Dump();
    pq.ExtractMin().Dump();
    pq.Count.Dump();
    pq.ExtractMin().Dump(); // should throw
}
