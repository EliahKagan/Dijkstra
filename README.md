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
single-source shortest paths on a graph whose order and edges you specify. It
also contains a graph generator tool, if you want to specify a randomly
generated graph.

&ldquo;Dijkstra&rdquo; supports a several data structures to hold vertex-cost
pairs, and several forms of output to report the results, including one that
uses the GraphViz `dot` command to draw the graph in a manner that reveals the
paths.

## License

This program is licensed under [0BSD](https://spdx.org/licenses/0BSD.html). See
[`LICENSE`](LICENSE).

## How to Use

&ldquo;Dijkstra&rdquo; is a Windows program. It is written as a
[LINQPad](https://www.linqpad.net/) query. You&rsquo;ll need to install LINQPad
if you don&rsquo;t have it. Then open `dijkstra.linq` in LINQPad 6 and run the
query.

The interface is fairly intuitive, but a few thigns may be non-obvious:

- You&rsquo;ll want to install GraphViz to get the `dot` command, so
  &ldquo;Dijkstra&rdquo; can produce nice pictures. On some systems with some
  ways of installing GraphViz, you may need to run `dot -c` after installing.
- The graph generator dialog box (if you chooose to use that) sometimes opens
  in the background. This is a bug, which I haven&rsquo;t fully fixed yet.
- It is not always immediately clear that you need to scroll down to see
  results.

A more detailed usage guide follows.

### Specify the graph

A small graph is specified by default for demonstration purpose. If you like,
you can try it out with that. You may want to try out different source
vertices.

Alternatively, you can manually change or replace the graph description with
your own:

- Put the number of vertices in the graph in *order*. The vertices will be
  numbered from 0 to one less than the order of the graph.
- Specify your edges. The format is one edge per line, with each edge written
  as a three integers, separated by spaces. The first two integers are the
  source and destination vertices, and the third is the weight. All weights
  must be nonnegative. (Unlike some other algorithms&mdash;particularly
  Bellman-Ford&mdash;Dijkstra&rdquo;s algorithm doesn&rsquo;t support negative
  edge weights, even if there are no negative cycles.)

A third option is to randomly generate a graph.

- Click *Generate a graph&hellip;* at the top. The *Graph Generator* window
  should appear. It may start in the background (this is a bug).
- Specify the order (number of vertices), size (number of edges), and range of
  weights. Or leave them at the defaults. (The defaults generate a graph a
  little bigger than the one that appears when you first run the program.)
- Decide if you want to allow loops (self-edges), parallel edges (edges that
  share both a source and destination vertex, but may be of different weights),
  and if you want to insist weights be unique. Adjust the checkboxes
  accordingly.
- If you want high-quality pseudorandom number generation&mdash;this uses a
  cryptographic PRNG, but the results in this application should not be used
  for anything security sensitive&mdash;check the box for that.

If a graph with the parameters you&rsquo;ve specified is possible, then the
status line says &ldquo;OK&rdquo; and you can click *Generate*. Otherwise, the
status line will tell you what&rsquo;s wrong. (For example, the size of a graph
is at most the square of its order, unless you allow parallel edges.)

The graph generator populates the *Order* and *Edge* textareas in the main UI.

### Choose your priority queue data structure

Dijkstra&rsquo;s algorithm has different performance
characteristics&mdash;including different asymptotic runtimes&mdash;depending
on what data structure is chosen as the [priority
queue](https://en.wikipedia.org/wiki/Priority_queue).

