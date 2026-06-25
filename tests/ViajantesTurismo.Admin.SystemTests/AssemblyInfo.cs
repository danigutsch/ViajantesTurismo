// Admin system tests share AppHost, browser, and database resources; internal parallelism caused CI resource contention.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
