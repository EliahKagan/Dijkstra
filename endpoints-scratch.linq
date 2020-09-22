<Query Kind="Statements" />

static Func<ulong, (int src, int dest)> CreateEndpointsDecoder(ulong order)
{
    return encodedEndpoints => {
        var src = encodedEndpoints / (order - 1);
        var dest = encodedEndpoints % (order - 1);
        if (src <= dest) ++dest;
        return (src: (int)src, dest: (int)dest);
    };
}

var decode = CreateEndpointsDecoder(6);

(from i in Enumerable.Range(0, 30)
 select (ulong)i into encodedEndpoints
 let result = decode(encodedEndpoints)
 select new { encodedEndpoints, result.src, result.dest }).Dump();
