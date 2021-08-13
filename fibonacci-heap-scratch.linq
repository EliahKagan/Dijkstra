<Query Kind="Program" />

#load "./dijkstra.linq"

private static void Main()
{
    IPriorityQueue<string, int> pq = new FibonacciHeap<string, int>();
    pq.InsertOrDecrease("bar", 48);
    pq.InsertOrDecrease("foo", 23);
    pq.InsertOrDecrease("baz", 90);
    pq.InsertOrDecrease("quux", 4);
    pq.InsertOrDecrease("foobar", 100);
    pq.InsertOrDecrease("a", 11);
    pq.InsertOrDecrease("b", 13);
    pq.InsertOrDecrease("c", 12);
    pq.InsertOrDecrease("ham", 200);
    pq.InsertOrDecrease("baz", 3);
    pq.Dump();
    pq.InsertOrDecrease("spam", 150);
    //pq.InsertOrDecrease("ham", 10);
    pq.ExtractMin().Dump();
    pq.ExtractMin().Dump();
}
