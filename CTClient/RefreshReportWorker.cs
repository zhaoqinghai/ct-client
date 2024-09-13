using CTService;
using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;

namespace CTClient
{
    public class RefreshReportWorker : IRunServer
    {
        public async Task Run()
        {
            StdSchedulerFactory factory = new StdSchedulerFactory();
            IScheduler scheduler = factory.GetScheduler().Result;

            IJobDetail job = JobBuilder.Create<RefreshReportJob>()
                               .WithIdentity("RefreshReportJob", "FreshReportGroup")
                               .Build();

            ITrigger trigger = TriggerBuilder.Create()
                              .WithIdentity("FreshReportGroupTrigger", "FreshReportGroup")
                              .WithCronSchedule("30 0 0,8,20 * * ?")
                              .Build();
            await scheduler.Start();
            await scheduler.ScheduleJob(job, trigger);
            await Task.Delay(Timeout.InfiniteTimeSpan);
        }
    }

    public class RefreshReportJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            WeakReferenceMessenger.Default.Send(new RefreshReportEvent());
            return Task.CompletedTask;
        }
    }

    public class RefreshReportEvent
    {
    }
}