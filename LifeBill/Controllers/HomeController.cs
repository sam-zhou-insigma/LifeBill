﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections;
using System.Data;
using LifeBill.Models;
using LifeBill.Models.Interface;
using LifeBill.Models.Services;
using System.Web.Security;
using LifeBill.Models.Util;

namespace LifeBill.Controllers
{
    public class HomeController : Controller
    {
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string name, string pswd)
        {
            if (!PubUtil.IsDangerous(name)) {
                return View();
            }

            IUsers iUser = new UserServices();

            DataTable dt = iUser.SelectUserByNameAndPswd(HttpUtility.HtmlEncode(name), PubUtil.GetMd5(HttpUtility.HtmlEncode(pswd)));

            if (dt.Rows.Count > 0)
            {
                string cook = Keys.USERID + ":" + dt.Rows[0]["id"].ToString() + ", " +
                            Keys.LOGIN + ":" + dt.Rows[0]["login"].ToString() + ", " +
                            Keys.NAME + ":" + dt.Rows[0]["name"].ToString() + ", " +
                            Keys.TEL + ":" + dt.Rows[0]["tel"].ToString() + ", " +
                            Keys.EMAIL + ":" + dt.Rows[0]["email"].ToString() + ", " +
                            Keys.SEX + ":" + dt.Rows[0]["sex"].ToString() + ", " +
                            Keys.ADDTIME + ":" + dt.Rows[0]["addtime"].ToString();

                FormsAuthentication.SetAuthCookie(cook, true);
                
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [Authorize]
        [HttpGet]
        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Login", "Home");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Add()
        {
            IBill iBill = new BillServices();

            DataTable dt = iBill.SelectTagListByIsShow("Y");

            ViewBag.Tags = dt;

            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Add(FormCollection form)
        {
            string[] type = form["type"].Split(',');
            string[] price = form["price"].Split(',');
            string[] notes = form["notes"].Split(',');

            int year = Convert.ToInt32(form["year"]);
            int month = Convert.ToInt32(form["month"]);
            int day = Convert.ToInt32(form["day"]);

            List<string> list = new List<string>();

            IBill iBill = new BillServices();

            //获取入账的类型
            DataTable tags = iBill.SelectTagByType("I");

            double revenue = 0;        //入账
            double outlay = 0;         //出账

            for (int i = 0; i < type.Length; i++)
            {
                double temp = 0;

                for (int j = 0; j < tags.Rows.Count; j++)
                {
                    if (type[i] == tags.Rows[j]["id"].ToString())
                    {
                        revenue += Convert.ToDouble(price[i]);
                        temp = 0;
                        break;
                    }
                    else
                    {
                        temp = Convert.ToDouble(price[i]);
                    }
                }

                outlay += temp;
            }

            //拼接主档SQL
            string sql = String.Format("insert into billmaster(years, months, days, revenue, outlay, addtime, userid) values({0}, {1}, {2}, {3}, {4}, now(), 1)", year, month, day, revenue, outlay);

            list.Add(sql);

            //拼接明细档
            for (int i = 0; i < type.Length; i++)
            {
                sql = String.Format("insert into billdetail(masterid, tagid, price, notes, addtime) " +
                                "values((select id from billmaster where years={0} and months={1} and days={2}), {3}, {4}, '{5}', now())", year, month, day, type[i], price[i], notes[i].Trim());

                list.Add(sql);
            }

            iBill.InsertBill(list);

            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Upd()
        {
            int mid = Convert.ToInt32(Request["i"]);

            IBill iBill = new BillServices();

            DataTable tags = iBill.SelectTagListByIsShow("Y");

            DataTable dets = iBill.SelectDetailByMasterId(mid);

            ViewBag.Tags = tags;

            ViewBag.Details = dets;

            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Upd(FormCollection form)
        {
            int mid = Convert.ToInt32(Convert.ToDouble(form["id"])); 
            
            string[] type = form["type"].Split(',');
            string[] price = form["price"].Split(',');
            string[] notes = form["notes"].Split(',');

            List<string> list = new List<string>();

            IBill iBill = new BillServices();

            //获取入账的类型
            DataTable tags = iBill.SelectTagByType("I");

            double revenue = 0;        //入账
            double outlay = 0;         //出账

            for (int i = 0; i < type.Length; i++)
            {
                double temp = 0;

                for (int j = 0; j < tags.Rows.Count; j++)
                {
                    if (type[i] == tags.Rows[j]["id"].ToString())
                    {
                        revenue += Convert.ToDouble(price[i]);
                        temp = 0;
                        break;
                    }
                    else
                    {
                        temp = Convert.ToDouble(price[i]);
                    }
                }

                outlay += temp;
            }

            //拼接主档SQL
            string sql = String.Format("update billmaster set revenue={0}, outlay={1}, addtime=now() where id={2}", revenue, outlay, mid);

            list.Add(sql);

            //删除明细档
            sql = String.Format("delete from billdetail where masterid={0} ", mid);

            list.Add(sql);

            //拼接明细档
            for (int i = 0; i < type.Length; i++)
            {
                sql = String.Format("insert into billdetail(masterid, tagid, price, notes, addtime) " +
                                "values({0}, {1}, {2}, '{3}', now())", mid, type[i], price[i], notes[i].Trim());

                list.Add(sql);
            }

            iBill.InsertBill(list);

            return RedirectToAction("Index");
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Tagchart()
        {
            return View();
        }

        /// <summary>
        /// 按季度查询
        /// </summary>
        /// <returns></returns>
        public ActionResult Quarter() {
            return View();
        }

        public ContentResult GetQuarter() {
            int year = Convert.ToInt32(Request["y"]);
            string type = Request["t"];
            string num = Request["n"];

            string sql = "";

            if (type == "week")
            {
                if (num == "0")
                {
                    sql += String.Format("SELECT sum(outlay) as price, week(concat(years, '-', months, '-', days))+1 as names FROM billmaster where years={0} group by names", year);
                }
                else 
                {
                    sql += String.Format("select t.tagname as names, sum(d.price) as price from billdetail d, billtags t where d.tagid = t.id " +
                                "and d.masterid in (SELECT id FROM billmaster where years={0} and week(concat(years, '-', months, '-', days))+1 = {1} ) and t.tagtype = 'O' " +
                                "group by t.tagname", year, num);
                }
            }
            else if (type == "month")
            {
                if (num == "0")
                {
                    sql += String.Format("SELECT sum(outlay) as price, month(concat(years, '-', months, '-', days)) as names FROM billmaster where years={0} group by names", year);
                }
                else
                {
                    sql += String.Format("select t.tagname as names, sum(d.price) as price from billdetail d, billtags t where d.tagid = t.id " +
                                "and d.masterid in (SELECT id FROM billmaster where years={0} and month(concat(years, '-', months, '-', days)) = {1} ) and t.tagtype = 'O' " +
                                "group by t.tagname", year, num);
                }
            }
            else if (type == "quarter") 
            {
                if (num == "0")
                {
                    sql += String.Format("SELECT sum(outlay) as price, quarter(concat(years, '-', months, '-', days)) as names FROM billmaster where years={0} group by names", year);
                }
                else
                {
                    sql += String.Format("select t.tagname as names, sum(d.price) as price from billdetail d, billtags t where d.tagid = t.id " +
                                "and d.masterid in (SELECT id FROM billmaster where years={0} and quarter(concat(years, '-', months, '-', days)) = {1} ) and t.tagtype = 'O' " +
                                "group by t.tagname", year, num);
                }
            }

            IBill iBill = new BillServices();

            DataTable dt = iBill.SelectBillBySql(sql);

            string res = "";

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (i != dt.Rows.Count - 1)
                {
                    if (i == 1)
                    {
                        res += "{name: '" + dt.Rows[i]["names"].ToString() + "', y: " + dt.Rows[i]["price"].ToString() + ", sliced: true, selected: true}, ";
                    }
                    else
                    {
                        res += "['" + dt.Rows[i]["names"].ToString() + "', " + dt.Rows[i]["price"].ToString() + "], ";
                    }
                }
                else
                {
                    res += "['" + dt.Rows[i]["names"].ToString() + "', " + dt.Rows[i]["price"].ToString() + "]";
                }
            }

            return Content(res);
        }

        public ContentResult GetTagchart()
        { 
            int year = Convert.ToInt32(Request["y"]);

            int month = Convert.ToInt32(Request["m"]);

            string sql = String.Format("select t.tagname as tagname, sum(d.price) as price from billdetail d, billtags t where d.tagid=t.id and d.masterid in (select id from billmaster where years={0} and months={1}) and d.tagid not in (select id from billtags where tagtype='I') group by d.tagid", year, month);

            IBill iBill = new BillServices();

            DataTable dt = iBill.SelectBillBySql(sql);

            string res = "";

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (i != dt.Rows.Count - 1)
                {
                    if(i == 1)
                    {
                        res += "{name: '" + dt.Rows[i]["tagname"].ToString() + "', y: " + dt.Rows[i]["price"].ToString() + ", sliced: true, selected: true}, ";
                    }
                    else
                    {
                        res += "['" + dt.Rows[i]["tagname"].ToString() + "', " + dt.Rows[i]["price"].ToString() + "], ";
                    }
                }
                else
                {
                    res += "['" + dt.Rows[i]["tagname"].ToString() + "', " + dt.Rows[i]["price"].ToString() + "]";
                }
            }

            return Content(res);
        }

        public ContentResult GetMap()
        {
            int year = Convert.ToInt32(Request["y"]);

            int month = Convert.ToInt32(Request["m"]); 

            string sql = String.Format("select id, concat('-', outlay) as outlay, revenue, days from billmaster where years={0} and months={1} group by years, months, days", year, month);

            IBill iBill = new BillServices();

            DataTable dt = iBill.SelectBillBySql(sql);

            string res = "";

            string r1 = "[";
            string r2 = "{name: '出账', data: [";
            string r3 = "{name: '进账', data: [";

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (i != dt.Rows.Count - 1)
                {
                    r1 += "'" + dt.Rows[i]["days"].ToString() + "', ";
                    r2 += "" + dt.Rows[i]["outlay"].ToString() + ", ";
                    r3 += "" + dt.Rows[i]["revenue"].ToString() + ", ";
                }
                else
                {
                    r1 += "'" + dt.Rows[i]["days"].ToString() + "'";
                    r2 += "" + dt.Rows[i]["outlay"].ToString() + "";
                    r3 += "" + dt.Rows[i]["revenue"].ToString() + "";
                
                }
            }

            r1 += "]";
            r2 += "]}";
            r3 += "]}";

            res = r1 + "~*~" + r2 + "~*~" + r3;

            return Content(res);
        }

        public ContentResult GetJson()
        { 
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            if (Request["y"] != null&& Request["m"] != null)
            {
                try
                {
                    year = Convert.ToInt32(Request["y"]);
                    month = Convert.ToInt32(Request["m"]);
                }
                catch (Exception ex)
                {
                    year = DateTime.Now.Year;
                    month = DateTime.Now.Month;
                }
            }
            
            var str = SetJson(year, month);

            return Content(str);
        }

        private string SetJson(int year, int month)
        {
            IBill iBill = new BillServices();

            DataTable dt = iBill.SelectMasterByDate(year, month);

            string json = "{";

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    json += "\"_" + dt.Rows[i]["date"] + "\":[{\"total\":\"" + dt.Rows[i]["total"] + "\", \"id\":\""+dt.Rows[i]["ID"]+"\"}], ";
                }
            }

            return json + "}";
        }

    }
}
