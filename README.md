<!--
  Copyright (C) 2021 Eliah Kagan <degeneracypressure@gmail.com>

  Permission to use, copy, modify, and/or distribute this software for any
  purpose with or without fee is hereby granted.

  THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
  REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
  AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
  INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
  LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
  OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
  PERFORMANCE OF THIS SOFTWARE.
-->

# Dijkstra - Visualizing Dijkstra&rsquo;s algorithm with various priority queues

This program runs [Dijkstra&rsquo;s
algorithm](https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm) to compute
[single-source shortest
paths](https://en.wikipedia.org/wiki/Shortest_path_problem#Single-source_shortest_paths)
on a weighted directed graph whose order and edges you specify. It also
contains a graph generator tool, if you want to specify a randomly generated
graph.

&ldquo;Dijkstra&rdquo; supports a several data structures to hold vertex-cost
pairs, and several forms of output to report the results, including one that
uses the [GraphViz](https://graphviz.org/) `dot` command to draw the graph in a
manner that reveals the paths.

## License

This program is licensed under [0BSD](https://spdx.org/licenses/0BSD.html). See
[`LICENSE`](LICENSE).

## How to Run &ldquo;Dijkstra&rdquo;

&ldquo;Dijkstra&rdquo; is a Windows program. It is written as a
[LINQPad](https://www.linqpad.net/) query.

1. You&rsquo;ll need to install LINQPad 6 if you don&rsquo;t have it.
2. [Install GraphViz](https://graphviz.org/download/) to get the `dot` command,
   so &ldquo;Dijkstra&rdquo; can produce nice pictures. On some systems with
   some ways of installing GraphViz, you may need to run `dot -c` after
   installing.
3. **Open `dijkstra.linq` in LINQPad and run the query.** On most screens,
   output will be easier to read if you arrange panels vertically
   (<kbd>Ctrl</kbd>+<kbd>F8</kbd> toggles this in LINQPad).

See the tips below. You may also be interested in the [detailed usage
guide](#usage-guide).

## Tips (a.k.a. bugs I haven&rsquo;t fixed)

The program&rsquo;s interface is fairly intuitive, but a few things may be
non-obvious:

- The [graph generator](#specify-the-graph) dialog box (if you choose to use
  that) sometimes opens in the background. This is a bug, which I haven&rsquo;t
  fully fixed yet.
- It is not always immediately clear when you need to scroll down to see
  results.
- [Inconsistent results using different priority queues don&rsquo;t necessarily
  mean there is a bug.](#a-note-on-consistency) (A related bug, though, is that
  the program&rsquo;s interface wrongly makes it seem like consistency should
  always be expected.)

## Goals

This program has two major goals:

### Pretty pictures

The &ldquo;graph drawing&rdquo; output format draws the least-cost paths tree
from the source vertex to every other vertex that can be reached from it. (All
edges are drawn; edges in the least-cost paths tree are red while unused edges
are black.)

This tree is the parents tree that Dijkstra&rsquo;s algorithm produces, but
with edges pointing out from parents instead of into them (i.e., it is the
transpose of that tree). This is an illuminating and&mdash;in my opinion&mdash;
pleasing way to view the paths, at least if the graph is not too big.

### Demonstration of various priority queues

Besides [looking cool](#pretty-pictures), the main point of this program is to
demonstrate how Dijkstra&rsquo;s algorithm can be understood as a class of
algorithms parameterized by the choice of priority queue data structure.

## Reading the Code

LINQPad shows the code in a left pane (or an upper pane if panels are arranged
horizontally).

Dijkstra&rsquo;s algorithm is implemented in `Graph.ComputeShortestPaths`. It
uses a priority queue supplied via dependency injection. The priority queue
must implement the `IPriorityQueue` interface.

The priority queue implementations that are available for selection are:

- `UnsortedPriorityQueue`, a naive priority queue
- `SortedSetPriorityQueue`, a red-black tree (currently implemented via
  `System.Collections.Generic.TreeSet`)
- `BinaryHeap`, a binary minheap + map data structure
- `FibonacciHeap`, a Fibonacci heap + map data structure

See [Choose your priority queue data
structure](#Choose-your-priority-queue-data-structure) below for further
algorithmic details.

&ldquo;Dijkstra&rdquo; is currently split into three C# query files, named with
`.linq` suffixes, but all the code mentioned in this section is in the main
source code file, `dijkstra.linq`. (This is to make it easy to quickly read and
experiment with the algorithms while re-running the query and seeing the
results.)

## Usage Guide

### Specify the graph

A small graph is specified by default for demonstration purpose. If you like,
you can try it out with that. You may want to try out different source
vertices.

Alternatively, you can manually change or replace the graph description with
your own:

1. Put the number of vertices in the graph in *order*. The vertices will be
   numbered from 0 to one less than the order of the graph.
2. Specify your edges. The format is one edge per line, with each edge written
   as a three integers, separated by spaces. The first two integers are the
   source and destination vertices, and the third is the weight. All weights
   must be nonnegative. (Unlike some other algorithms&mdash;particularly
   Bellman-Ford&mdash;Dijkstra&rdquo;s algorithm doesn&rsquo;t support negative
   edge weights, even if there are no negative cycles.)

A third option is to randomly generate a graph.

1. Click *Generate a graph&hellip;* at the top. The *Graph Generator* window
   should appear. It may start in the background (this is a bug).
2. Specify the order (number of vertices), size (number of edges), and range of
   weights. Or leave them at the defaults. (The defaults generate a graph a
   little bigger than the one that appears when you first run the program.)
3. Decide if you want to allow loops (self-edges), parallel edges (edges that
   share both a source and destination vertex, but may be of different
   weights), and if you want to insist weights be unique. Adjust the checkboxes
   accordingly.
4. If you want high-quality pseudorandom number generation&mdash;this uses a
   cryptographic PRNG, but the results in this application should not be used
   for anything security sensitive&mdash;check the box for that.

If a graph with the parameters you&rsquo;ve specified is possible, then the
status line says &ldquo;OK&rdquo; and you can click *Generate*. Otherwise, the
status line will tell you what&rsquo;s wrong. (For example, the size of a graph
is at most the square of its order, unless you allow parallel edges.)

The graph generator populates the *Order* and *Edges* textareas in the main UI.

### Choose your priority queue data structure(s)

Dijkstra&rsquo;s algorithm has different performance
characteristics&mdash;including different asymptotic runtimes&mdash;depending
on what data structure is chosen as the [priority
queue](https://en.wikipedia.org/wiki/Priority_queue).

&ldquo;Dijkstra&rdquo; currently supports four priority queue implementations.
All are enabled by default. To use fewer of them, uncheck some of the
&ldquo;Priority queues&rdquo; checkboxes. At least one must be enabled, to run
Dijkstra&rsquo;s algorithm. When more than one used, the program runs
Dijkstra&rsquo;s algorithm separately with each kind of priority queue. Results
are reported and compared.

Dijkstra&rsquo;s algorithm with different priority queues does not always find
the same paths. See [A note on
&ldquo;consistency&rdquo;](#a-note-on-consistency) below.

The supported priority queues, and their (amortized) asymptotic worst-case
running times for the most relevant operations, are:

#### Unsorted priority queue

This is a naive priority queue + map implementation. It uses a hash table to
store vertex-cost mappings. Runtimes are:

- *O(1)* insert (&ldquo;push&rdquo;) and decrease-key.
- *O(V)* extract-min (&ldquo;pop&rdquo;).

Dijkstra&rsquo;s algorithm does up to *O(E)* insert or decrease-key operations
and *O(V)* extract-min operations, for a total runtime of ***O(V<sup>2</sup> +
E)***. Assuming no (or few) parallel edges, this is *O(V<sup>2</sup>)*.

If the graph is also dense, then this is *O(E)* and such a data structure can
be reasonable from a performance perspective, though this implementation has
unnecessarily large constants, which I think is because it uses a hash table
implemented using linked structures (`System.Collections.Generic.Dictionary`).
I implemented this priority queue&mdash;and the others&mdash; generically,
accepting keys of arbitrary type, even though this program only uses integers
in a contiguous range starting from a zero. That restriction can be leveraged
to implement a straightforward array-based flat-map that should perform better.

This is a poor choice of priority queue for sparse graphs (unless *|V|* is very
small, in which case the asymptotic runtime is unimportant).

#### Red-black tree

This is a self-balancing binary search tree. Currently it is implemented in
terms of `System.Collections.Generic.SortedSet`, but it should really use a
tree *multi*set instead. Since `SortedSet` is not a multiset, my comparator
breaks ties based on the vertex numbers. This works fine, but it&rsquo;s
inelegant. (It also may affect the results, though not in a way that makes them
wrong. See [A note on &ldquo;consistency&rdquo;](#a-note-on-consistency)
below.)

`SortedSet` is a red-black tree. When I move to using a multiset, I&rsquo;ll
probably continue using a red-black tree, but I might use an AVL tree, splay
tree, or some other self-balancing binary search tree. This does not affect
asymptotic worst-cast runtimes.

Runtimes are:

- *O(log(V))* insert (&ldquo;push&rdquo;) and decrease-key.
- *O(log(V))* extract-min (&ldquo;pop&rdquo;).

Dijkstra&rsquo;s algorithm does up to *O(E)* insert or decrease-key operations
and *O(V)* extract-min operations, for a total runtime of ***O((V + E) log
V)***.

If the graph has at least as many edges as vertices, this is *O(E log V)*. If
the graph is, furthermore, dense, but with no (or few) parallel edges, that can
be written as *O(V<sup>2</sup> log V)*. If the graph is very sparse, it&rsquo;s
*O(V log V)*.

#### Binary heap

This is a [binary minheap](https://en.wikipedia.org/wiki/Binary_heap) + map
data structure. It is the most commonly used data structure for
Dijsktra&rsquo;s algorithm (as well as for Prim&rsquo;s algorithm), because:

- Compared to a [Fibonacci heap](#fibonacci-heap), it has worse asymptotic
  runtime, but that is a complicated linked data structure, so its constants
  are larger and a binary heap tends to perform better except for large dense
  graphs.
- Compared to a [self-balancing BST](#red-black-tree), it has the same
  asymptotic runtime, but because traversals and rotations have large
  constants, the binary minheap&mdash;which is implemented using an array even
  though conceptually it has a tree structure&mdash;is faster.

As with the [red-black tree](#red-black-tree) detailed above, asymptotic
runtimes are:

- *O(log(V))* insert (&ldquo;push&rdquo;) and decrease-key.
- *O(log(V))* extract-min (&ldquo;pop&rdquo;).

Dijkstra&rsquo;s algorithm does up to *O(E)* insert or decrease-key operations
and *O(V)* extract-min operations, for a total runtime of ***O((V + E) log
V)***.

If the graph has at least as many edges as vertices, this is *O(E log V)*. If
the graph is, furthermore, dense, but with no (or few) parallel edges, that can
be written as *O(V<sup>2</sup> log V)*. If the graph is very sparse, it&rsquo;s
*O(V log V)*.

#### Fibonacci heap

This is a [Fibonacci minheap](https://en.wikipedia.org/wiki/Fibonacci_heap) +
map data structure. It provides the best known asymptotic runtime of any data
structure for Dijkstra&rsquo;s algorithm (and the related Prim&rsquo;s
algorithm) in the general case, and the least uncommonly implemented of several data structures with that asymptotic complexity.

- Compared to a [binary heap](#binary-heap), the Fibonacci heap has better
  asymptotic runtime, but that is a simple data structure that, even though it
  is conceptually a tree, can (and in practice always is) implemented as a flat
  array, so its constants are smaller and it tends to perform better than a
  Fibonacci heap except for large dense graphs.
- The Fibonacci heap&rsquo;s runtime [is matched
  by](https://en.wikipedia.org/wiki/Heap_(data_structure)#Comparison_of_theoretic_bounds_for_variants)
  some other data structures that are even more complex and esoteric, such as a
  [Brodal queue](https://en.wikipedia.org/wiki/Brodal_queue).
- In special cases&mdash;such as integer weights such that the maximum cost of
  any simple path has a strictly limited range&mdash;there are other data
  structures, such as a [van Emde Boas
  tree](https://en.wikipedia.org/wiki/Van_Emde_Boas_tree), with better
  asymptotic runtime.

The Fibonacci heap&rsquo;s (amortized) runtimes are:

- *O(1)* insert (&ldquo;push&rdquo;) and decrease-key.
- *O(log(V))* extract-min (&ldquo;pop&rdquo;).

Dijkstra&rsquo;s algorithm does up to *O(E)* insert or decrease-key operations
and *O(V)* extract-min operations, for a total runtime of ***O(E + V log V)***.

If the graph is dense, but with no (or few) parallel edges, that can be written
as *O(E)* or as *O(V<sup>2</sup>)*. If the graph is very sparse, it&rsquo;s
*O(V log V)*.

The asymptotic runtime of the Fibonacci heap is thus always at least as good as
both an unsorted priority queue and a binary heap even for the specific cases
where they work best (so it is strictly better than either in the general
case), though its large constants often make it an inferior choice in practice.

### Choose your form(s) output

By default, &ldquo;Dikstra&rdquo; shows output as a &ldquo;graph
drawing,&rdquo; though a few other forms of output are available. They are
controlled by the checkboxes under *Output*. Any combination may be chosen,
though if you uncheck all of them then the program assumes this is a mistake
and will refuse to run Dijkstra&rsquo;s algorithm without reporting any
results.

The supported forms of output are:

#### Parents table

This is a direct representation of the data structure Dijkstra&rsquo;s
algorithm returns: a table showing the best predecessor/parent vertex for
getting to each vertex in the graph from the specified source. The source
vertex, as well as any vertices that are not reachable from it, have *null* as
their parent vertex.

This is compact. The size of the table is proportional to the order of the
graph (the number of vertices). So you may prefer this form of output for large
dense graphs.

#### Edge selection

This is a table of all the edges in the entire graph, including edges that are
not on any shortest path from the given source to any other vertex. It contains
a *Mark* field that is *true* for edges that appear on one of the shortest
paths and *false* for edges that do not.

The size of this table is proportional to the size of the graph (the number of
edges). I included this because it represents an intermediate form of the data,
used to generate the other two forms of output detailed below, but I kept it in
since it can occasionally be useful or interesting even outside debugging, and
since LINQPad allows it (like other tables) to be exported for use in other
applications.

#### DOT code

<!-- FIXME: write this part -->

### Graph drawing

<!-- FIXME: write this part -->

## A note on &ldquo;consistency&rdquo;

Most of the time, the results will be the same with all priority queues. But
this is not guaranteed&mdash;many graphs have more than one choice of shortest
path from a source vertex to one or more destination vertices. So if the
results are reported as not being &ldquo;consistent&rdquo;, that doesn&rsquo;t
necessarily mean there is a bug or other problem.

If you want to *deliberately* try and create a situation where different
priority queue data structures will yield different results, I suggest making a
dense graph with many duplicate edge weights (though it can happen even in
sparse but redundant graphs with unique weights).

The factors that determine which shortest paths Dijkstra&rsquo;s algorithm will
find, when there is more than one possible choice, are at least as related to
small implementation details of the priority queues as they are to larger
differences. For example, two binary minheap implementations could make
different choices as to which child in the heap to pick in the sift-down
operation when their values (costs so far) are equal. This affects what vertex
is likely to be extracted sooner, and thus which paths are likely to be found
first and preferred. It is the difference between `<` and `<=` (or `>` and
`>=`) in a place where either happens to be fully acceptable.

So if you notice a difference between (for example) results from a binary
minheap and a Fibonacci heap, you&rsquo;d have to look in detail at how they
came about before assuming the difference is conceptually illuminating.

Although different results with different priority queues do not indicate a
bug, and none of the instances in which I have produced this have been bugs,
that of course does *not* ensure my implementations are bug-free.

## Acknowledgements

<!-- FIXME: write this section -->

## Future Directions

<!-- FIXME: write this section -->
