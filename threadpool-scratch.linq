<Query Kind="Statements">
  <Namespace>LINQPad.Controls</Namespace>
</Query>

// Copyright (C) 2020 Eliah Kagan <degeneracypressure@gmail.com>
//
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
//
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
// WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
// SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
// WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION
// OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN
// CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

#nullable enable

var go = new Button("Go!", sender => new {
        sender.IsMultithreaded,
        Thread.CurrentThread.ManagedThreadId,
        Thread.CurrentThread.IsThreadPoolThread
    }.Dump());

var multithreaded =
    new CheckBox(text: nameof(go.IsMultithreaded),
                 isChecked: false,
                 onClick: sender => go.IsMultithreaded = sender.Checked);

const string demoComment = @"
To fire an event, a LINQPad control checks its IsMultithreaded property. If
true, it invokes handlers asynchronously on a worker thread (in the managed
thread pool). If false, it invokes handlers synchronously on the main thread.
This property can be changed even after the control is dumped.";

var demoPanel = new StackPanel(horizontal: true,
                               go, new Label("  "), multithreaded);

new StackPanel(horizontal: false,
               new Label(demoComment.Trim()),
               new FieldSet("Try it out!", demoPanel))
    .Dump("How LINQPad controls run event handlers");

const string exceptionComment = @"
When an exception is thrown on the main thread, LINQPad dumps it, while also
indicating where it occurred in the editor. The query process remains loaded
and the query can continue to be used. But what about uncaught asynchronous
exceptions?
";

var throwExceptionOnWorkerThread =
    new Button("Throw Exception on Worker Thread", delegate {
        var id = Thread.CurrentThread.ManagedThreadId;
        var worker = Thread.CurrentThread.IsThreadPoolThread;
        "Throwing exception...".Dump();
        throw new NotSupportedException(
                $"Thread {id}. On thread pool? {worker}");
    }) { IsMultithreaded = true };

new StackPanel(horizontal: false,
               new Label(exceptionComment.Trim()),
               new FieldSet("Find out!", throwExceptionOnWorkerThread))
    .Dump("Asynchronous exceptions");
