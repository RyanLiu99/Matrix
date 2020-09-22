# Matrix - high performance network access and calculation.

* Requirement is the code can have job done in < 30s. This code can do it in  3.5s

* To tune on different machine, please adjust  Program.batchSize.


# Key points
    
- For I/O operation, it uses await number of batch Tasks (not await single task which will be very slow) to run paralla, and use partiton to limit parallaism to avoid overwhelming network.
        
  - Use Enumerable.Range to run create tasks paralla;
  - Use await.WhenAll to limit parallism.

- For CPU operation, it uses Parallel.For/ParallelOptions to speed up.  It can do so because each calculation is independ since result array is pre-created. 