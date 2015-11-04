﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using CodeComb.vNextExperimentCenter.Models;
using CodeComb.vNextExperimentCenter.ViewModels;

namespace CodeComb.vNextExperimentCenter.Controllers
{
    public class ContestController : BaseController
    {
        public IActionResult Index()
        {
            ViewBag.InProgress = DB.Contests
                .Where(x => x.Begin <= DateTime.Now && DateTime.Now <= x.End)
                .OrderBy(x => x.End)
                .ToList();
            return PagedView(DB.Contests.Where(x => x.End <= DateTime.Now || DateTime.Now <= x.Begin).OrderByDescending(x => x.Begin), 5);
        }

        [Route("Contest/{id}")]
        public IActionResult Show(string id)
        {
            var ret = DB.Contests
                .Include(x => x.Experiments)
                .ThenInclude(x => x.Experiment)
                .Where(x => x.Id == id)
                .SingleOrDefault();
            if (ret == null)
                return Prompt(x =>
                {
                    x.Title = "资源没有找到";
                    x.Details = "您请求的资源没有找到，请返回重试！";
                    x.StatusCode = 404;
                });
            var statistics = new List<ContestStatistics>();
            var number = 'A';
            foreach (var x in ret.Experiments.OrderBy(y => y.Point))
            {
                var s = new ContestStatistics
                {
                    Title = x.Contest.Title,
                    Point = x.Point,
                    Submitted = DB.Statuses.Where(y => y.Time >= ret.Begin && y.Time < ret.End).Count(),
                    Accepted = DB.Statuses.Where(y => y.Time >= ret.Begin && y.Time < ret.End && y.Result == StatusResult.Successful).Count(),
                    ExperimentId = x.ExperimentId,
                    Number = number++
                };
                try
                {
                    s.Average = DB.Statuses.Where(y => y.Time >= ret.Begin && y.Time < ret.End).Average(y => y.Total > 0 ? (y.Accepted * x.Point / y.Total) : 0);
                }
                catch
                {
                }
                if (User.IsSignedIn())
                {
                    var last = DB.Statuses.Where(y => y.Time >= ret.Begin && y.Time < ret.End && y.UserId == User.Current.Id).LastOrDefault();
                    if (last == null)
                        s.Flag = "";
                    else if (last.Result == StatusResult.Successful)
                        s.Flag = "Successful";
                    else
                    {
                        if (last.Total == 0)
                            s.Flag = "0";
                        else
                            s.Flag = Convert.ToInt32(last.Accepted * x.Point / last.Total).ToString();
                    }
                }
                statistics.Add(s);
            }
            ViewBag.Statistics = statistics;
            return View(ret);
        }

        [Route("Contest/{id}/Rank")]
        public IActionResult Rank(string id)
        {
            var ret = DB.Contests
                .Include(x => x.Experiments)
                .ThenInclude(x => x.Experiment)
                .Where(x => x.Id == id)
                .SingleOrDefault();
            if (ret == null)
                return Prompt(x =>
                {
                    x.Title = "资源没有找到";
                    x.Details = "您请求的资源没有找到，请返回重试！";
                    x.StatusCode = 404;
                });
            var exps = new List<ContestProblem>();
            var number = 'A';
            foreach(var x in ret.Experiments.OrderBy(x => x.Point))
            {
                exps.Add(new ContestProblem
                {
                    Id = x.ExperimentId,
                    Number = number++,
                    Point = x.Point,
                    Title = x.Experiment.Title
                });
            }
            ViewBag.Experiments = exps;
            var statuses = DB.Statuses
                .Include(x => x.User)
                .Where(x => x.Time >= ret.Begin && x.Time < ret.End && x.ExperimentId != null)
                .ToList();
            var rank = new List<ContestRank>();
            foreach(var x in statuses.DistinctBy(x => x.UserId))
            {
                var rankitem = new ContestRank();
                rankitem.User = x.User;
                foreach(var y in exps)
                {
                    var last = statuses.Where(z => z.UserId == x.UserId && z.ExperimentId == y.Id).LastOrDefault();
                    if (last != null)
                    {
                        rankitem.Details[y.Number] = new ContestRankDetail
                        {
                            Point =  last.Total == 0 ? 0 : last.Accepted * y.Point / last.Total,
                            TimeUsage = last.TimeUsage
                        }; 
                    }
                }
                rank.Add(rankitem);
            }
            rank = rank.OrderByDescending(x => x.Point).ThenBy(x => x.Time).ToList();
            ViewBag.Rank = rank;
            return View(ret);
        }

        [HttpGet]
        [AnyRoles("Root, Master")]
        [Route("Contest/{id}/Edit")]
        public IActionResult Edit(string id)
        {
            var ret = DB.Contests
                .Where(x => x.Id == id)
                .SingleOrDefault();
            if (ret == null)
                return Prompt(x =>
                {
                    x.Title = "资源没有找到";
                    x.Details = "您请求的资源没有找到，请返回重试！";
                    x.StatusCode = 404;
                });
            return View(ret);
        }

        [HttpPost]
        [AnyRoles("Root, Master")]
        [Route("Contest/{id}/Edit")]
        public IActionResult Edit(string id, Contest Model)
        {
            var ret = DB.Contests
                .Where(x => x.Id == id)
                .SingleOrDefault();
            if (ret == null)
                return Prompt(x =>
                {
                    x.Title = "资源没有找到";
                    x.Details = "您请求的资源没有找到，请返回重试！";
                    x.StatusCode = 404;
                });
            ret.Description = Model.Description;
            ret.Begin = Model.Begin;
            ret.End = Model.End;
            ret.Title = Model.Title;
            DB.SaveChanges();
            return Prompt(x =>
            {
                x.Title = "修改成功";
                x.Details = "比赛信息已经修改成功！";
            });
        }

        [HttpGet]
        [AnyRoles("Root, Master")]
        [Route("Contest/{id}/Experiment")]
        public IActionResult Experiment(string id)
        {
            var ret = DB.Contests
               .Include(x => x.Experiments)
               .ThenInclude(x => x.Experiment)
               .Where(x => x.Id == id)
               .SingleOrDefault();
            if (ret == null)
                return Prompt(x =>
                {
                    x.Title = "资源没有找到";
                    x.Details = "您请求的资源没有找到，请返回重试！";
                    x.StatusCode = 404;
                });
            return View(ret);
        }

        [HttpPost]
        [AnyRoles("Root, Master")]
        [Route("Contest/{id}/Experiment/Delete")]
        public IActionResult DeleteExperiment(string id, long eid)
        {
            var exp = DB.ContestExperiments
                .Where(x => x.ContestId == id && x.ExperimentId == eid)
                .SingleOrDefault();
            if (exp == null)
                return Prompt(x =>
                {
                    x.Title = "资源没有找到";
                    x.Details = "您请求的资源没有找到，请返回重试！";
                    x.StatusCode = 404;
                });
            DB.ContestExperiments.Remove(exp);
            DB.SaveChanges();
            return RedirectToAction("Experiment", "Contest", new { id = id });
        }

        [HttpPost]
        [AnyRoles("Root, Master")]
        [Route("Contest/{id}/Experiment/Add")]
        public IActionResult AddExperiment(string id, long ExperimentId, int Point)
        {
            var ret = DB.Contests
               .Where(x => x.Id == id)
               .SingleOrDefault();
            if (ret == null)
                return Prompt(x =>
                {
                    x.Title = "资源没有找到";
                    x.Details = "您请求的资源没有找到，请返回重试！";
                    x.StatusCode = 404;
                });
            if (DB.ContestExperiments.Where(x => x.ExperimentId == ExperimentId && x.ContestId == id).Count() > 0)
                return Prompt(x =>
                {
                    x.Title = "添加失败";
                    x.Details = "本次比赛中已经包含了该题目，请勿重复添加！";
                    x.StatusCode = 400;
                });
            DB.ContestExperiments.Add(new ContestExperiment
            {
                ContestId = id,
                ExperimentId = ExperimentId,
                Point = Point
            });
            DB.SaveChanges();
            return RedirectToAction("Experiment", "Contest", new { id = id });
        }
    }
}
