using System.Reflection;

namespace AntSign
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(Util.GetBeiJingTimeStr() + " - Start running...");

            await Run();

            Console.WriteLine(Util.GetBeiJingTimeStr() + " - End running...");

#if DEBUG
            Console.WriteLine("Program Run On Debug");
            Console.ReadLine();
#else
            Console.WriteLine("Program Run On Release");
#endif
        }

        static async Task Run()
        {
            string bark_Devicekey = Util.GetEnvValue("BARK_DEVICEKEY");
            string bark_Icon = Util.GetEnvValue("BARK_ICON");

            var interfaceType = typeof(IGetQA);
            var implementingTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
                .ToList();

            implementingTypes.Sort((x, y) =>
            {
                var orderX = x.GetCustomAttribute<ImplementationOrderAttribute>()?.Order ?? 999;
                var orderY = y.GetCustomAttribute<ImplementationOrderAttribute>()?.Order ?? 999;
                return orderX.CompareTo(orderY);
            });

            List<string> message_all = [];

            DateTime date = DateTime.Now.Date;

            foreach (var type in implementingTypes)
            {
                //Console.WriteLine("");
                //Console.WriteLine(type.Name);
                //if (type.Name == "GetQA_DuoTe_35822")
                //{
                //    continue;
                //}
                //if (type.Name == "GetQA_ALi213_371835")
                //{
                //    continue;
                //}
                //if (type.Name == "GetQA_M_ALi213_371835")
                //{
                //    continue;
                //}
                //if (type.Name == "GetQA_xuexili_jinridaan")
                //{
                //    continue;
                //}

                var specificImplementation = Activator.CreateInstance(type) as IGetQA;
                (List<string> anwsers, List<string> fulls) = await specificImplementation.GetQA(date);

                for (int i = 0; i < anwsers.Count; i++)
                {
                    if (anwsers[i].StartsWith("答案") == false)
                        anwsers[i] = $"答案：" + anwsers[i];
                }

                if (anwsers.Count > 0)
                {
                    message_all.Add("只展示答案↓↓↓↓↓↓↓↓↓↓");
                    message_all.AddRange(ClearHtmlCode(anwsers));
                    message_all.Add("");
                    message_all.Add("完整信息↓↓↓↓↓↓↓↓↓↓");
                    message_all.AddRange(ClearHtmlCode(fulls));
                    break;
                }
            }

            if (message_all.Count == 0)
            {
                Console.WriteLine("Not Data");
                return;
            }

            string title = "蚂蚁庄园今日答案";
            string content = string.Join("\n", message_all);

#if DEBUG
            Console.WriteLine(content);
#endif

            Console.WriteLine("Send");
            await SendUtil.SendBark(bark_Devicekey, bark_Icon, title, content);
        }

        private static List<string> ClearHtmlCode(List<string> lst)
        {
            for (int i = 0; i < lst.Count; i++)
                lst[i] = lst[i].Replace("&nbsp;", " ").Replace("&ldquo;", "“").Replace("&rdquo;", "”").Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();

            return lst;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ImplementationOrderAttribute(int order) : Attribute
    {
        public int Order { get; } = order;
    }

    public interface IGetQA
    {
        Task<(List<string> anwsers, List<string> fulls)> GetQA(DateTime date);
    }

    [ImplementationOrder(1)]
    public class GetQA_DuoTe_35822 : IGetQA
    {
        public async Task<(List<string> anwsers, List<string> fulls)> GetQA(DateTime date)
        {
            string url = "https://m.duotegame.com/mgl/35822.html";
            var document = await HttpClientHelper.HttpHtml(url);

            List<string> anwsers = [];
            List<string> fulls = [];

            int max = 30;
            string todayno1 = date.Month + "." + date.Day;
            string todayno2 = date.Month + "月" + date.Day + "日";
            var ps = document.DocumentNode.SelectNodes("//p");
            if (ps != null)
            {
                for (int i = 0; i < ps.Count; i++)
                {
                    if (i > max)
                        break;

                    string q = ps[i].InnerText.Trim();
                    if (q.StartsWith(todayno1) || q.StartsWith(todayno2))
                    {
                        if (i + 1 <= ps.Count)
                        {
                            string a = ps[i + 1].InnerText.Trim();
                            if (string.IsNullOrWhiteSpace(a) == false)
                            {
                                anwsers.Add(a);
                                fulls.Add(q);
                                fulls.Add(a);
                            }
                        }
                    }
                }
            }

            return (anwsers, fulls);
        }
    }

    [ImplementationOrder(2)]
    public class GetQA_ALi213_371835 : IGetQA
    {
        public async Task<(List<string> anwsers, List<string> fulls)> GetQA(DateTime date)
        {
            string url = "https://app.ali213.net/mip/gl/371835.html";
            var document = await HttpClientHelper.HttpHtml(url);

            List<string> anwsers = [];
            List<string> fulls = [];

            int max = 30;
            string todayno1 = "【" + date.Month + "." + date.Day + "】";
            var ps = document.DocumentNode.SelectNodes("//p");
            if (ps != null)
            {
                for (int i = 0; i < ps.Count; i++)
                {
                    if (i > max)
                        break;

                    string dt = ps[i].InnerText.Trim();
                    if (dt.StartsWith(todayno1))
                    {
                        if (i + 1 <= ps.Count)
                        {
                            var p = ps[i + 1];
                            var span = p.SelectSingleNode("span");
                            if (span != null)
                            {
                                var strong = span.SelectSingleNode("strong");
                                if (strong != null)
                                {
                                    anwsers.Add(strong.InnerText.Trim());
                                    fulls.Add(p.InnerText.Trim());
                                }
                                else
                                {
                                    anwsers.Add(span.InnerText.Trim());
                                    fulls.Add(p.InnerText.Trim());
                                }
                            }
                        }
                        if (i + 2 <= ps.Count)
                        {
                            var p = ps[i + 2];
                            var span = p.SelectSingleNode("span");
                            if (span != null)
                            {
                                var strong = span.SelectSingleNode("strong");
                                if (strong != null)
                                {
                                    anwsers.Add(strong.InnerText.Trim());
                                    fulls.Add(p.InnerText.Trim());
                                }
                                else
                                {
                                    anwsers.Add(span.InnerText.Trim());
                                    fulls.Add(p.InnerText.Trim());
                                }
                            }
                        }
                    }
                }
            }

            return (anwsers, fulls);
        }
    }

    [ImplementationOrder(3)]
    public class GetQA_M_ALi213_371835 : IGetQA
    {
        public async Task<(List<string> anwsers, List<string> fulls)> GetQA(DateTime date)
        {
            string url = "https://m.ali213.net/news/gl1910/371835.html";
            var document = await HttpClientHelper.HttpHtml(url);

            List<string> anwsers = [];
            List<string> fulls = [];

            int max = 30;
            string todayno1 = "【" + date.Month + "." + date.Day + "】";
            var ps = document.DocumentNode.SelectNodes("//p");
            if (ps != null)
            {
                for (int i = 0; i < ps.Count; i++)
                {
                    if (i > max)
                        break;

                    string dt = ps[i].InnerText.Trim();
                    if (dt.StartsWith(todayno1))
                    {
                        if (i + 1 <= ps.Count)
                        {
                            var p = ps[i + 1];
                            var span = p.SelectSingleNode("span");
                            if (span != null)
                            {
                                var strong = span.SelectSingleNode("strong");
                                if (strong != null)
                                {
                                    anwsers.Add(strong.InnerText.Trim());
                                    fulls.Add(p.InnerText.Trim());
                                }
                                else
                                {
                                    anwsers.Add(span.InnerText.Trim());
                                    fulls.Add(p.InnerText.Trim());
                                }
                            }
                        }
                        if (i + 2 <= ps.Count)
                        {
                            var p = ps[i + 2];
                            var span = p.SelectSingleNode("span");
                            if (span != null)
                            {
                                var strong = span.SelectSingleNode("strong");
                                if (strong != null)
                                {
                                    anwsers.Add(strong.InnerText.Trim());
                                    fulls.Add(p.InnerText.Trim());
                                }
                                else
                                {
                                    anwsers.Add(span.InnerText.Trim());
                                    fulls.Add(p.InnerText.Trim());
                                }
                            }
                        }
                    }
                }
            }

            return (anwsers, fulls);
        }
    }

    [ImplementationOrder(4)]
    public class GetQA_xuexili_jinridaan : IGetQA
    {
        public async Task<(List<string> anwsers, List<string> fulls)> GetQA(DateTime date)
        {
            string url = "http://www.xuexili.com/mayizhuangyuan/jinridaan.html";
            var document = await HttpClientHelper.HttpHtml(url);

            List<string> anwsers = [];
            List<string> fulls = [];

            int max = 30;
            string todayno1 = $"{date.Year}年{date.Month}月{date.Day}日";
            var trs = document.DocumentNode.SelectNodes("//tr");
            if (trs != null)
            {
                for (int i = 0; i < trs.Count; i++)
                {
                    if (i > max)
                        break;

                    var tds = trs[i].SelectNodes("td");
                    if (tds != null && tds.Count == 3)
                    {
                        if (tds[0].InnerText.Contains(todayno1))
                        {
                            var span1 = tds[2].SelectSingleNode("span");
                            if (span1 != null)
                            {
                                var span2 = span1.SelectSingleNode("span");
                                if (span2 != null)
                                {
                                    anwsers.Add(span2.InnerText.Trim());
                                }
                                else
                                {
                                    anwsers.Add(span1.InnerText.Trim());
                                }
                            }
                            else
                            {
                                anwsers.Add(tds[2].InnerText.Trim());
                            }
                            fulls.Add(tds[1].InnerText.Trim());
                        }
                    }
                }
            }

            return (anwsers, fulls);
        }
    }
}
