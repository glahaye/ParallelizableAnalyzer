# ParallelizableAnalyzer

A code analyzer to detect potential opportunities to parallelize async tasks

It works by detecting method definitions containing "await" expressions and identifies candidates for parallelization.

The idea is to transform code such as:

```C#
// Wait for both tasks to complete - Total time is sum of the two tasks
await TaskThatTakesTwoSeconds();
await TaskThatTakesThreeSeconds();
```

into code such as:

```C#
taskThatTakesTwoSecondsTask = TaskThatTakesTwoSeconds();
taskThatTakesThreeSecondsTask = TaskThatTakesThreeSeconds();

// Wait for both tasks to complete - Total time is the max of the two tasks
await Task.WhenAll(taskThatTakesTwoSecondsTask, taskThatTakesThreeSecondsTask);
```
