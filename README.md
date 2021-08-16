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
[LINQPad](https://www.linqpad.net/) query. Currently it works with .NET Core
3.0, .NET Core 3.1, and .NET 5 and uses whichever you have set as the default
in LINQPad.

1. You&rsquo;ll need to install LINQPad 6 if you don&rsquo;t have it.
2. [Install GraphViz](https://graphviz.org/download/) to get the `dot` command,
   so &ldquo;Dijkstra&rdquo; can produce nice pictures. On some systems with
   some ways of installing GraphViz, you may need to run `dot -c` after
   installing.
3. **Open `dijkstra.linq` in LINQPad and run the query.** On most screens,
   output will be easier to read if you arrange panels vertically
   (<kbd>Ctrl</kbd>+<kbd>F8</kbd> toggles this in LINQPad).

See [Tips](#tips-known-bugs) and [Other Bugs](#other-bugs). You may also be
interested in the [Usage Guide](#usage-guide).

## Tips

The program&rsquo;s interface is fairly intuitive, but a few things may be
non-obvious:

- The [graph generator](#specify-the-graph) dialog box (if you choose to use
  that) sometimes opens in the background. This is a bug, which I haven&rsquo;t
  fully fixed yet.
- It is not always immediately clear when you need to scroll down to see
  results. This is an area where the UI might be improved.
- Dijkstra&rsquo;s algorithm with different priority queues can produce
  different results. This can happen when two or more different paths from a
  source exist to the same destination vertices with the same minimal cost. See
  [A note on &ldquo;consistency&rdquo;](#a-note-on-consistency).

## Other Bugs

The graph generator has a very serious bug: its user interface is difficult to
use, and sometimes entirely unusable, when [display
scaling](https://support.microsoft.com/en-us/topic/make-text-and-apps-bigger-c3095a80-6edd-4779-9282-623c4d721d64)
(of more than 100%) is used. This makes the graph generator unusable on most
ultra HD displays (where at least 200% display scaling is common). This would
also be a serious accessibility problem for many users of screens of any
resolution.

The output would be much more interesting if it included timings for
Dijkstra&rsquo;s algorithm from each data structure. Troubleshooting `dot`,
especially in corner cases, would be easier if full error output from `dot`
were shown. These two features are implemented on an experimental branch, but
would need to be backported.

See [Future Directions](#future-directions).

## Goals

This program has two major goals:

### Pretty pictures

The &ldquo;graph drawing&rdquo; form of output draws the least-cost paths tree
from the source vertex to every other vertex that can be reached from it. (All
edges are drawn; edges in the least-cost paths tree are red while unused edges
are black.)

This tree is the parents tree that Dijkstra&rsquo;s algorithm produces, but
with edges pointing out from parents instead of into them (i.e., it is the
transpose of that tree). This is an illuminating and&mdash;in my opinion&mdash;
pleasing way to view the paths, at least if the graph is not too big.

See [Graph drawing](#graph-drawing).

### Demonstration of various priority queues

Besides [looking cool](#pretty-pictures), the main point of this program is to
demonstrate how Dijkstra&rsquo;s algorithm can be understood as a class of
algorithms parameterized by the choice of priority queue data structure.

See [Reading the Code](#reading-the-code) below.

## Reading the Code

LINQPad shows the code in a left pane (or an upper pane if panels are arranged
horizontally).

Dijkstra&rsquo;s algorithm is implemented in `Graph.ComputeShortestPaths`. It
uses a priority queue supplied via dependency injection. The priority queue
must implement an interface, `IPriorityQueue`, which exposes priority queue
operations useful for implementing Dijsktra&rsquo;s algorithm.

The priority queue implementations that are available for selection are:

- [`UnsortedPriorityQueue`](#unsorted-priority-queue), a naive priority queue
- [`SortedSetPriorityQueue`](#red-black-tree), a [red-black
  tree](https://en.wikipedia.org/wiki/Red-black_tree) (currently implemented
  via
  [`System.Collections.Generic.SortedSet`](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedset-1?view=netcore-3.1))
- [`BinaryHeap`](#binary-heap), a [binary
  minheap](https://en.wikipedia.org/wiki/Binary_heap) + map data structure
- [`FibonacciHeap`](#fibonacci-heap), a [Fibonacci
  heap](https://en.wikipedia.org/wiki/Fibonacci_heap) + map data structure

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

A small graph is specified by default for demonstration purposes. If you like,
you can try running Dijkstra&rsquo;s algorithm on that. You may want to try out
different source vertices.

Alternatively, you can manually change or replace the graph description with
your own:

1. Put the number of vertices in the graph in *order*. The vertices will be
   numbered from 0 to one less than the order of the graph.
2. Specify your edges. The format is one edge per line, with each edge written
   as three integers, separated by spaces. The first two integers are the
   source and destination vertices, respectively, and the third is the weight.
   All weights must be nonnegative. (Unlike some other
   algorithms&mdash;particularly
   [Bellman-Ford](https://en.wikipedia.org/wiki/Bellman%E2%80%93Ford_algorithm)&mdash;Dijkstra&rsquo;s
   algorithm doesn&rsquo;t support negative edge weights, even if there are no
   negative cycles.)

A third option is to randomly generate a graph.

1. Click *Generate a graph&hellip;* at the top. The *Graph Generator* window
   should appear. It may start in the background (this is a bug).
2. Specify the order (number of vertices), size (number of edges), and range of
   weights. Or leave them at the defaults. (The defaults generate a graph a
   little bigger than the one that appears when you first run the program.)
3. Decide if you want to allow loops (self-edges), if you want to allow
   parallel edges (edges that share both a source and destination vertex, but
   may be of different weights), and if you want to insist weights be unique.
   Adjust the checkboxes accordingly.
4. If you want high-quality pseudorandom number generation (this uses a
   cryptographic PRNG, but the results in this application should not be used
   for anything sensitive), check the box for that.

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
All are enabled by default. To use fewer of them, uncheck some of the *Priority
queues* checkboxes. At least one must be enabled, to run Dijkstra&rsquo;s
algorithm. When more than one used, the program runs Dijkstra&rsquo;s algorithm
separately with each kind of priority queue. Results are reported and compared.

Running Dijkstra&rsquo;s algorithm with different priority queues does not
always find all the same shortest paths. See [A note on
&ldquo;consistency&rdquo;](#a-note-on-consistency) below.

The supported priority queues, and their (amortized) asymptotic worst-case
running times for the most relevant operations, are:

#### Unsorted priority queue

This is a naive priority queue + map implementation. It uses a hash table to
store vertex-cost mappings. Except that it uses as hash table rather than an
array, it is the [priority-queue
analogue](https://en.wikipedia.org/wiki/Priority_queue#Equivalence_of_priority_queues_and_sorting_algorithms)
of [selection sort](https://en.wikipedia.org/wiki/Selection_sort). Runtimes
are:

- *O(1)* insert (&ldquo;push&rdquo;) and decrease-key.
- *O(V)* extract-min (&ldquo;pop&rdquo;).

Dijkstra&rsquo;s algorithm does up to *O(E)* insert or decrease-key operations
and *O(V)* extract-min operations, for a total runtime of ***O(V<sup>2</sup> +
E)***. Assuming no (or few) parallel edges, this is *O(V<sup>2</sup>)*.

If the graph is also dense, then this is *O(E)* and such a data structure can
be reasonable from a performance perspective, though this implementation has
unnecessarily large constants, which I think is because it uses a hash table
implemented using linked structures
([`System.Collections.Generic.Dictionary`](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=netcore-3.1)).
I implemented this priority queue&mdash;and the others&mdash;generically,
accepting keys of arbitrary type, even though this program only uses integers
in a contiguous range starting from a zero. That restriction can be leveraged
to implement a straightforward array-based flat-map that should perform better.

This is a poor choice of priority queue for sparse graphs (unless *|V|* is very
small, in which case the asymptotic runtime is unimportant).

#### Red-black tree

This is a [self-balancing binary search
tree](https://en.wikipedia.org/wiki/Self-balancing_binary_search_tree).
Currently it is implemented in terms of
[`System.Collections.Generic.SortedSet`](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedset-1?view=netcore-3.1),
but it should really use a tree *multi*set instead. Since `SortedSet` is not a
multiset, my comparator breaks ties based on the vertex numbers. This works
fine, but it&rsquo;s inelegant. (It also may affect the results, though not in
a way that makes them wrong. See [A note on
&ldquo;consistency&rdquo;](#a-note-on-consistency) below.)

`SortedSet` [is
implemented](https://source.dot.net/#System.Collections/System/Collections/Generic/SortedSet.cs,11)
as a [red-black tree](https://en.wikipedia.org/wiki/Red%E2%80%93black_tree).
When I move to using a multiset, I&rsquo;ll probably continue using a red-black
tree, but I might use an [AVL tree](https://en.wikipedia.org/wiki/AVL_tree),
[splay tree](https://en.wikipedia.org/wiki/Splay_tree), or some other
self-balancing binary search tree; the name of this option in the UI would then
change accordingly. None of this affects asymptotic worst-case runtimes.

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
  asymptotic runtime, but because
  [traversals](https://en.wikipedia.org/wiki/Tree_traversal) and
  [rotations](https://en.wikipedia.org/wiki/Tree_rotation) have large
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
algorithm) in the general case. It is the most commonly (well, least
uncommonly) implemented of several data structures with that asymptotic
complexity.

- Compared to a [binary heap](#binary-heap), the Fibonacci heap has better
  asymptotic runtime. But the binary minheap is a much simpler data structure
  that, even though it is conceptually a tree, can (and in practice always is)
  implemented as a flat array. So its constants are smaller and it tends to
  perform better than a Fibonacci heap except for large dense graphs.
- The Fibonacci heap&rsquo;s runtime [is matched
  by](https://en.wikipedia.org/wiki/Heap_(data_structure)#Comparison_of_theoretic_bounds_for_variants)
  some other data structures that are even more complex and esoteric, such as a
  [Brodal queue](https://en.wikipedia.org/wiki/Brodal_queue).
- In special cases&mdash;such as integer weights where the maximum cost of any
  simple path has a strictly limited range&mdash;there are other data
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
both an unsorted priority queue and a binary heap, even for the specific cases
where each of those works best. So it is strictly better than either in the
general case, asymptotically speaking. But its large constants often make it an
inferior choice in practice.

### Choose your form(s) of output

By default, &ldquo;Dijkstra&rdquo; shows output as a &ldquo;graph
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
a *Mark* field that is *true* for edges that appear on some shortest path and
*false* for edges that do not. Together these edges make up a tree whose simple
paths from the source vertex are all the shortest paths to vertices reachable
from the source.

The size of this table is proportional to the size (the number of edges) of the
graph. I included this because it represents an intermediate form of the data,
used to generate the other two forms of output detailed below; but I kept it
in, since it can occasionally be useful or interesting even outside debugging,
and because LINQPad allows it (like other tables) to be exported for use in
other applications.

#### DOT code

This is [DOT code](https://graphviz.org/doc/info/lang.html) describing a graph
on which the tree of all shortest paths from the source will be shown with its
edges in red. That is, this is a machine-readable (and fairly human-readable)
description of the graph that [graph drawing](#graph-drawing) actually draws.

This does not include geometric information about where or how vertices, edges,
and their labels should be drawn. GraphViz&rsquo;s `dot` command generates that
automatically from this. (It&rsquo;s possible to include such information in
DOT code, but &ldquo;Dijkstra&rdquo; doesn&rsquo;t and wouldn&rsquo;t benefit
from doing so.)

#### Graph drawing

This draws the graph, with edges of the tree of all least-cost paths from the
source vertex to all other reachable vertices colored red to distinguish them
(from other edges, colored black).

[As mentioned above](#pretty-pictures), this least-cost paths tree is
effectively what Dijkstra&rsquo;s algorithm emits&mdash;though it really emits
a parents tree, which the least-cost paths tree transposes.

To find a shortest path from the source vertex to any other vertex, follow red
edges from the source to the destination. If the destination is reachable from
the source, there is exactly one simple path along red edges from the source to
the destination, and that path is one of the shortest paths. (There may be more
than one shortest path from the source to the destination, and if so, different
implementations [may yield different results](#a-note-on-consistency), but
within each result, there will be only one path shown in red edges from the
source to each reachable destination.) If the destination is not reachable from
the source, then no such path is shown, with red edges or otherwise.

The image is generated as an SVG and dumped into the results panel. It is
created by feeding [generated DOT code](#dot-code) to the `dot` command. That
command is part of GraphViz. It is not necessary to enable *DOT code* output;
if the checkbox for DOT code is unchecked, it will still be generated behind
the scenes to produce the graph drawing.

For graphs with hundreds of vertices and thousands of edges, the resulting
image may take up a lot of visual space. For even larger graphs, `dot` may take
a very long time to run. So you might decide to uncheck *Graph drawing* in such
cases. The work done by `dot` to lay out a graph is, by far, the most
computationally intensive part of &ldquo;Dijkstra&rdquo;&rsquo;s functionality.
([Future Directions](#future-directions) mentions a possible way graph-drawing
performance [might be improved](#msagl) in a later version.)

### Run the computation

&ldquo;Dijkstra&rdquo;&rsquo;s interface has *Run* and *Clear* buttons under
where you specify the graph and make priority queue and output choices.

Click *Run* to run Dijkstra&rsquo;s algorithm on the input. The algorithm is
then run separately with each kind of priority queue selected. Identical
results are grouped together and all groups are shown. Usually there is just
one group; that is, usually Dijkstra&rsquo;s algorithm finds the same shortest
paths with any of the priority queue data structures implemented in this
program. [But sometimes the results are different.](#a-note-on-consistency)

If you attempt to generate a [graph drawing](#graph-drawing) showing the result
of Dijkstra&rsquo;s algorithm on a graph with thousands of edges or more, it
may take some time. &ldquo;Dijkstra&rdquo; doesn&rsquo;t currently support
reliable cancellation of external commands, but you can terminate the `dot`
process (`dot.exe` in the Task Manager).

The *Run* button that is part of &ldquo;Dijkstra&rdquo;&rsquo;s interface
should **not** be confused with the &#9654; (&ldquo;Run&rdquo; / <kbd>F5</kbd>)
button that is part of LINQPad&rsquo;s own interface and is used to run or
re-run queries such as the &ldquo;Dijkstra&rdquo; program itself.

Clicking *Clear* clears the output and keeps (technically, redraws) your graph
description, source vertex choice, and choices of priority queues and forms of
output.

## A note on &ldquo;consistency&rdquo;

Most of the time, the results will be the same with all priority queues. But
this is not guaranteed&mdash;many graphs have more than one choice of shortest
path from a source vertex to one or more destination vertices. So if the
results are reported as not being &ldquo;consistent,&rdquo; that doesn&rsquo;t
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

### CLRS authors

I&rsquo;d like to thank [Thomas H. Cormen](https://www.cs.dartmouth.edu/~thc/),
[Charles E. Leiserson](https://www.csail.mit.edu/person/charles-e-leiserson),
[Ronald L. Rivest](https://people.csail.mit.edu/rivest/), and [Clifford
Stein](http://www.columbia.edu/~cs2035/). Although [the
code](#reading-the-code) of the [Fibonacci heap](#fibonacci-heap)
implementation in this program is not copied or directly translated from any
preexisting code, it is nonetheless strongly informed by, and to some extent
based on, the description of Fibonacci heaps in Chapter 19 of their famous book
*[Introduction to
Algorithms](https://en.wikipedia.org/wiki/Introduction_to_Algorithms)* (3rd
edition), including the very instructive pseudocode therein.

(On [the book&rsquo;s official
website](https://mitpress.mit.edu/books/introduction-algorithms-third-edition),
a few chapters are currently available for download, including a [chapter on
Fibonacci
heaps](http://mitp-content-server.mit.edu:18180/books/content/sectbyfn?collid=books_pres_0&id=8030&fn=Chapter%2020.pdf),
which relies on the preceding chapter on [binomial
heaps](http://mitp-content-server.mit.edu:18180/books/content/sectbyfn?collid=books_pres_0&id=8030&fn=Chapter%2019.pdf).
I hope that may be helpful&mdash;but it&rsquo;s not quite what I used. Although
I used the 3rd edition, and that website features the 3rd edition, I believe
those PDF chapters are actually from the 2nd edition. The preface to the 3rd
edition says, *&ldquo;We removed two chapters that were rarely taught: binomial
heaps and sorting networks.&hellip; The treatment of Fibonacci heaps no longer
relies on binomial heaps as a precursor.&rdquo;*)

### Jelani Nelson

The other source I found very helpful in learning about Fibonacci heaps was
[Advanced Algorithms (COMPSCI 224), Lecture
6](https://www.youtube.com/watch?v=gxp_FrgTkQI). I&rsquo;m thankful to [Jelani
Nelson](https://people.eecs.berkeley.edu/~minilek/), who taught that course and
delivered the lecture shown in that video.

### GraphViz authors

&ldquo;Dijkstra&rdquo; is significantly more useful, and much more fun, in the
presence of [GraphViz](https://graphviz.org/), whose `dot` command it uses to
generate [graph drawings](#graph-drawing). My thanks go to the
authors/contributors of GraphViz, as listed in the project&rsquo;s [Credits
page](https://graphviz.org/credits/).

## Future Directions

See also [Other Bugs](#other-bugs) above.

### Graph generator redesign

At minimum, the accessibility bug where the graph generator dialog
doesn&rsquo;t look right, and can even be utterly unusable, with > 100% display
scaling, should be fixed before this program can be considered of beta or
stable (rather than alpha) quality and before it should be recommended for
widespread use.

The basic layout is fine, but the implementation of that layout must be redone.
A detailed prototype that could be turned into an improved version is present
on the `graph-generator` branch, in the file `layout-scratch.linq`. Note that
the graph generator implemented in `generator.linq` does not (yet) carry that
modified design, even on that branch.

(A more extensive redesign/reimplementation could perhaps be done later, to use
a cross-platform toolkit rather than Windows Forms. This would lift one
impediment to making &ldquo;Dijkstra&rdquo; cross-platform, though its
dependency on LINQPad would still need to be addressed.)

### More responsive user interface

The interface sometimes becomes unresponsive for a short time when dealing with
large graphs. This main cause seems to be the interaction between
&ldquo;Dijkstra&rdquo; and LINQPad itself (since the program&rsquo;s graphical
interface elements, except for the graph generator dialog, appear in the
LINQPad results panel, and are thus actually rendered by LINQPad, via either
the
[`WebBrowser`](https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.webbrowser?view=net-5.0)
or [`WebView2`](https://docs.microsoft.com/en-us/microsoft-edge/webview2/)
control).

**The main work I&rsquo;ve done to try and eliminate that lag is on the `async`
branch.** I refactored much of the overall design of the code (though not most
of the lower-level details) and also made some parts
[asynchronous](https://docs.microsoft.com/en-us/dotnet/csharp/async). This
produced some improvement, but there was still sometimes some lag.

On that branch, I also added two other features:

- Each run of Dijkstra&rsquo;s algorithm is benchmarked, and the time it took
  to run is reported in a table that appears above any of the results.
- The `dot` runner reports when `dot` exited indicating failure and shows the
  full text written to the standard error stream.

The first of these features is quite nice and should really always have been
present. The second is less important but still handy. They are written in such
a way as to depend on other changes on this branch, but they could be
backported even if those other changes are not retained/used.

**I will probably not use this asynchronous approach.** It didn&rsquo;t
eliminate the lag, but a simpler approach did: making [the *Run*
button](#run-the-computation) do all its work on a worker thread on the managed
thread pool, instead of on the UI thread. Unlike the graph generator, which
uses [Windows Forms](https://en.wikipedia.org/wiki/Windows_Forms) and must make
all UI changes on the UI thread, all of the output (including any errors) from
running Dijkstra&rsquo;s algorithm and reporting the results is being
[marshaled across a process
boundary](https://www.linqpad.net/HowLINQPadWorks.aspx) to LINQPad to be
displayed, which is done in a thread-safe fashion.

That approach is now on the `master` branch (as well as the `fonts` and
`graph-generator` branches). But I think the refactor itself in `async` may be
worth keeping. I think the best approach for further work is to reexamine the
code and decide if that is the case and, if so, to keep the overall design but
convert the asynchronous methods to be ordinary synchronous methods instead, or
otherwise rewrite them in such a way as not to dispatch work to threads on the
managed thread pool (no `Task.Run`). The `master` branch and `async` branch (or
whatever branch implements these further changes&mdash;perhaps it will be
called `sync`) could then be merged.

Another consideration is that, since LINQPad 6.14.10, the results panel is
often rendered with
[`WebView2`/Edge](https://docs.microsoft.com/en-us/microsoft-edge/webview2/)
rather than
[`WebBrowser`/IE](https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.webbrowser?view=net-5.0).
It might be that there is less drop in responsiveness when `WebView2` is used.
Initial testing makes me suspect that, but I am far from sure. If so, then it
may or may not be worthwhile to fix the lag, depending on how many users have
the WebView2 runtime. I am not sure if LINQPad ships and installs it at this
point or not, but I believe Microsoft Edge will eventually supply it on all
Windows 10 systems.

There there disadvantages to making the *Run* button do its work on the managed
thread pool, mainly that detailed error reporting is made more complicated. The
`no-threadpool` branch does this work on the main thread (and synchronously).

### Faster *Edges* textarea population

LINQPad displays results in an embedded web browser:
[`WebBrowser`/IE](https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.webbrowser?view=net-5.0)
or
[`WebView2`/Edge](https://docs.microsoft.com/en-us/microsoft-edge/webview2/).
When the graph generator populates the *Edges* textarea with edges for a very
large graph, this takes a long time&mdash;far longer than actually generating
them takes, even when the high quality PRNG option is turned on.

Furthermore, interacting with the panel is slower after that, at least with
`WebBrowser`/IE, and with enough edges, LINQPad refuses to redraw the interface
when the *Clear* button (next to &ldquo;Run&rsquo; in the
&ldquo;Dijkstra&rdquo;) is clicked. I haven&rsquo;t worked on this problem, but
some redesign should probably be done to fix it.

When `WebView2`/Edge is used, a mitigation&mdash;which would also be a feature
improvement in other ways&mdash;may be to use a different text-box web control.
Perhaps [Monaco](https://microsoft.github.io/monaco-editor/) (the editing
control Visual Studio Code uses) could be used here.

### Better fonts

The contents of the *Order*, *Edges*, and *Source* input textareas&mdash;or at
least *Edges*&mdash;is effectively code. The contents of the *DOT code* output
textarea is literally code in the [DOT
language](https://graphviz.org/doc/info/lang.html). So the stylistic case for
these to be rendered in a monospaced font is fairly strong.

The `fonts` branch has such a change, but I&rsquo;m not convinced it looks
better, currently. The text also takes up more vertical space, which has the
effect of making the UI slightly less pleasant and moderately less intuitive.

### MSAGL

It may be valuable to add a second kind of graph drawing output, produced by
the  [Microsoft Automated Graph
Layout](https://github.com/microsoft/automatic-graph-layout) library. MSAGL is
very good at laying out large graphs quickly. I think this would make
visualization feasible for results on large inputs that currently would take
too long with `dot`.

### Unit Tests

I don&rsquo;t have unit tests for the priority queue implementations. This
would be nice, especially for the Fibonacci heap, since that data structure is
quite complicated and easy to get wrong. I don&rsquo;t think it has bugs that
cause it to be incorrect. But not thinking so isn&rsquo;t enough.

### The self-balancing BST should be a multiset

The [red-black tree](#red-black-tree) priority queue should be implemented as a
multiset. There are multiset implementations available via NuGet, but it might
be better to implement it for this purpose and have keys map to key-value node
references (as in the [Fibonacci heap](#fibonacci-heap) implementation).

### Other Priority Queues

Although there&rsquo;s not really ever any good reason to use a [Binomial
heap](https://en.wikipedia.org/wiki/Binomial_heap) for Dijkstra&rsquo;s
algorithm, it&rsquo;s conceptually relevant to understanding the Fibonacci
heap, so perhaps a binomial heap implementation should be included.

It would be good to have a [*d*-ary
heap](https://en.wikipedia.org/wiki/D-ary_heap), where the degree *d* is
adjusted per-run based on the graph&rsquo;s order and size. Like a binary heap,
this can be implemented in a flat array even though it is conceptually a tree.
I believe that can produce performance that rivals or exceeds that of a
Fibonacci heap, for any graph.

I&rsquo;m curious about Leonardo heaps. This is the [priority queue
analogue](https://en.wikipedia.org/wiki/Priority_queue#Equivalence_of_priority_queues_and_sorting_algorithms)
of [smoothsort](https://en.wikipedia.org/wiki/Smoothsort). I *think*
Dijkstra&rsquo;s algorithm with a Leonardo heap would have the same worst-case
asymptotic runtimes as with a binary heap. But elements in a Leonardo heap tend
to move around less in the array, during the routines that restore the heap
invariant. So operations are sometimes faster&mdash;at the expense of being
more complicated to implement&mdash;at least when used in smoothsort. Perhaps a
Leonardo heap would also be faster, on average, than a binary heap, for
Dijkstra&rsquo;s algorithm.

### Bellman-Ford

It would be nice to include a
[Bellman-Ford](https://en.wikipedia.org/wiki/Bellman%E2%80%93Ford_algorithm)
implementation for comparison.
