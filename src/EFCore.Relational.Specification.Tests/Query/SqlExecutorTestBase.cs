// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.TestModels.Northwind;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;

// ReSharper disable AccessToDisposedClosure
// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToConstant.Local
namespace Microsoft.EntityFrameworkCore.Query
{
    public abstract class SqlExecutorTestBase<TFixture> : IClassFixture<TFixture>
        where TFixture : NorthwindQueryRelationalFixture<NoopModelCustomizer>, new()
    {
        protected SqlExecutorTestBase(TFixture fixture) => Fixture = fixture;

        protected TFixture Fixture { get; }

        [Fact]
        public virtual void Executes_stored_procedure()
        {
            using (var context = CreateContext())
            {
                Assert.Equal(-1, context.Database.ExecuteSqlCommand(TenMostExpensiveProductsSproc));
            }
        }

        [Fact]
        public virtual void Executes_stored_procedure_with_parameter()
        {
            using (var context = CreateContext())
            {
                var parameter = CreateDbParameter("@CustomerID", "ALFKI");

                Assert.Equal(-1, context.Database.ExecuteSqlCommand(CustomerOrderHistorySproc, parameter));
            }
        }

        [Fact]
        public virtual void Executes_stored_procedure_with_generated_parameter()
        {
            using (var context = CreateContext())
            {
                Assert.Equal(-1, context.Database.ExecuteSqlCommand(CustomerOrderHistoryWithGeneratedParameterSproc, "ALFKI"));
            }
        }

        [Fact]
        public virtual void Throws_on_concurrent_command()
        {
            using (var context = CreateContext())
            {
                context.Database.EnsureCreated();

                using (var synchronizationEvent = new ManualResetEventSlim(false))
                {
                    using (var blockingSemaphore = new SemaphoreSlim(0))
                    {
                        var blockingTask = Task.Run(
                            () =>
                                context.Customers.Select(
                                    c => Process(c, synchronizationEvent, blockingSemaphore)).ToList());

                        var throwingTask = Task.Run(
                            () =>
                                {
                                    synchronizationEvent.Wait();
                                    Assert.Equal(
                                        CoreStrings.ConcurrentMethodInvocation,
                                        Assert.Throws<InvalidOperationException>(
                                            () => context.Database.ExecuteSqlCommand(@"SELECT * FROM ""Customers""")).Message);
                                });

                        throwingTask.Wait();

                        blockingSemaphore.Release(1);

                        blockingTask.Wait();
                    }
                }
            }
        }

        [Fact]
        public virtual void Query_with_parameters()
        {
            var city = "London";
            var contactTitle = "Sales Representative";

            using (var context = CreateContext())
            {
                var actual = context.Database
                    .ExecuteSqlCommand(
                        @"SELECT COUNT(*) FROM ""Customers"" WHERE ""City"" = {0} AND ""ContactTitle"" = {1}", city, contactTitle);

                Assert.Equal(-1, actual);
            }
        }

        [Fact]
        public virtual void Query_with_dbParameter_with_name()
        {
            var city = CreateDbParameter("@city", "London");

            using (var context = CreateContext())
            {
                var actual = context.Database
                    .ExecuteSqlCommand(
                        @"SELECT COUNT(*) FROM ""Customers"" WHERE ""City"" = @city", city);

                Assert.Equal(-1, actual);
            }
        }

        [Fact]
        public virtual void Query_with_positional_dbParameter_with_name()
        {
            var city = CreateDbParameter("@city", "London");

            using (var context = CreateContext())
            {
                var actual = context.Database
                    .ExecuteSqlCommand(
                        @"SELECT COUNT(*) FROM ""Customers"" WHERE ""City"" = {0}", city);

                Assert.Equal(-1, actual);
            }
        }

        [Fact]
        public virtual void Query_with_positional_dbParameter_without_name()
        {
            var city = CreateDbParameter(name: null, value: "London");

            using (var context = CreateContext())
            {
                var actual = context.Database
                    .ExecuteSqlCommand(
                        @"SELECT COUNT(*) FROM ""Customers"" WHERE ""City"" = {0}", city);

                Assert.Equal(-1, actual);
            }
        }

        [Fact]
        public virtual void Query_with_dbParameters_mixed()
        {
            var city = "London";
            var contactTitle = "Sales Representative";

            var cityParameter = CreateDbParameter("@city", city);
            var contactTitleParameter = CreateDbParameter("@contactTitle", contactTitle);

            using (var context = CreateContext())
            {
                var actual = context.Database
                    .ExecuteSqlCommand(
                        @"SELECT COUNT(*) FROM ""Customers"" WHERE ""City"" = {0} AND ""ContactTitle"" = @contactTitle", city, contactTitleParameter);

                Assert.Equal(-1, actual);

                actual = context.Database
                    .ExecuteSqlCommand(
                        @"SELECT COUNT(*) FROM ""Customers"" WHERE ""City"" = @city AND ""ContactTitle"" = {1}", cityParameter, contactTitle);

                Assert.Equal(-1, actual);
            }
        }

        [Fact]
        public virtual void Query_with_parameters_interpolated()
        {
            var city = "London";
            var contactTitle = "Sales Representative";

            using (var context = CreateContext())
            {
                var actual = context.Database
                    .ExecuteSqlCommand(
                        $@"SELECT COUNT(*) FROM ""Customers"" WHERE ""City"" = {city} AND ""ContactTitle"" = {contactTitle}");

                Assert.Equal(-1, actual);
            }
        }

        [Fact]
        public virtual async Task Executes_stored_procedure_async()
        {
            using (var context = CreateContext())
            {
                Assert.Equal(-1, await context.Database.ExecuteSqlCommandAsync(TenMostExpensiveProductsSproc));
            }
        }

        [Fact]
        public virtual async Task Executes_stored_procedure_with_parameter_async()
        {
            using (var context = CreateContext())
            {
                var parameter = CreateDbParameter("@CustomerID", "ALFKI");

                Assert.Equal(-1, await context.Database.ExecuteSqlCommandAsync(CustomerOrderHistorySproc, parameter));
            }
        }

        [Fact]
        public virtual async Task Executes_stored_procedure_with_generated_parameter_async()
        {
            using (var context = CreateContext())
            {
                Assert.Equal(-1, await context.Database.ExecuteSqlCommandAsync(CustomerOrderHistoryWithGeneratedParameterSproc, "ALFKI"));
            }
        }

        [Fact]
        public virtual async Task Throws_on_concurrent_command_async()
        {
            using (var context = CreateContext())
            {
                context.Database.EnsureCreated();

                using (var synchronizationEvent = new ManualResetEventSlim(false))
                {
                    using (var blockingSemaphore = new SemaphoreSlim(0))
                    {
                        var blockingTask = Task.Run(
                            () =>
                                context.Customers.Select(
                                    c => Process(c, synchronizationEvent, blockingSemaphore)).ToList());

                        var throwingTask = Task.Run(
                            async () =>
                                {
                                    synchronizationEvent.Wait();
                                    Assert.Equal(
                                        CoreStrings.ConcurrentMethodInvocation,
                                        (await Assert.ThrowsAsync<InvalidOperationException>(
                                            () => context.Database.ExecuteSqlCommandAsync(@"SELECT * FROM ""Customers"""))).Message);
                                });

                        await throwingTask;

                        blockingSemaphore.Release(1);

                        await blockingTask;
                    }
                }
            }
        }

        private static Customer Process(Customer c, ManualResetEventSlim e, SemaphoreSlim s)
        {
            e.Set();
            s.Wait();
            s.Release(1);
            return c;
        }

        [Fact]
        public virtual async Task Query_with_parameters_async()
        {
            var city = "London";
            var contactTitle = "Sales Representative";

            using (var context = CreateContext())
            {
                var actual = await context.Database
                    .ExecuteSqlCommandAsync(
                        @"SELECT COUNT(*) FROM ""Customers"" WHERE ""City"" = {0} AND ""ContactTitle"" = {1}", city, contactTitle);

                Assert.Equal(-1, actual);
            }
        }

        [Fact]
        public virtual async Task Query_with_parameters_interpolated_async()
        {
            var city = "London";
            var contactTitle = "Sales Representative";

            using (var context = CreateContext())
            {
                var actual = await context.Database
                    .ExecuteSqlCommandAsync(
                        $@"SELECT COUNT(*) FROM ""Customers"" WHERE ""City"" = {city} AND ""ContactTitle"" = {contactTitle}");

                Assert.Equal(-1, actual);
            }
        }

        protected NorthwindContext CreateContext() => Fixture.CreateContext();

        protected abstract DbParameter CreateDbParameter(string name, object value);

        protected abstract string TenMostExpensiveProductsSproc { get; }

        protected abstract string CustomerOrderHistorySproc { get; }

        protected abstract string CustomerOrderHistoryWithGeneratedParameterSproc { get; }
    }
}
