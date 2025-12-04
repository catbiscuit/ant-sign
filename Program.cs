using HtmlAgilityPack;
using System.Reflection;
using System.Text;

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

            for (int i = 0; i < 3; i++)
            {
                message_all = await GetQA(implementingTypes);

                if (message_all.Count > 0)
                    break;

                await Task.Delay(new Random(Guid.NewGuid().GetHashCode()).Next(876, 1234));
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

        static async Task<List<string>> GetQA(List<Type> implementingTypes)
        {
            List<string> message_all = [];

            DateTime date = DateTime.Now.Date;

            foreach (var type in implementingTypes)
            {
                try
                {
                    Console.WriteLine(type.Name);

                    //if (type.Name == "GetQA_DuoTe_35822")
                    //{
                    //    continue;
                    //}

                    var specificImplementation = Activator.CreateInstance(type) as IGetQA;

                    CancellationTokenSource cts = new(new TimeSpan(0, 0, 15));

                    (List<string> anwsers, List<string> fulls) = await specificImplementation.GetQA(date, cts.Token);

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
                catch (Exception ex)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("【异常类型】：" + ex?.GetType()?.Name);
                    sb.AppendLine("【异常信息】：" + ex?.Message);
                    sb.AppendLine("【堆栈调用】：" + ex?.StackTrace);
                    Console.WriteLine(sb.ToString());
                }
            }

            return message_all;
        }

        static List<string> ClearHtmlCode(List<string> lst)
        {
            for (int i = 0; i < lst.Count; i++)
                lst[i] = System.Web.HttpUtility.HtmlDecode(lst[i].Replace("&nbsp;", " ").Replace("&ldquo;", "“").Replace("&rdquo;", "”").Replace("\r", "").Replace("\n", "").Replace("\t", "")).Trim();

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
        Task<(List<string> anwsers, List<string> fulls)> GetQA(DateTime date, CancellationToken cancellationToken);
    }

    [ImplementationOrder(1)]
    public class GetQA_DuoTe_35822 : IGetQA
    {
        public async Task<(List<string> anwsers, List<string> fulls)> GetQA(DateTime date, CancellationToken cancellationToken)
        {
            string url = "https://m.duotegame.com/mgl/35822.html";
            var document = await HttpClientHelper.HttpHtml(url, true, cancellationToken);

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
    public class GetQA_M_ALi213_371835 : IGetQA
    {
        public async Task<(List<string> anwsers, List<string> fulls)> GetQA(DateTime date, CancellationToken cancellationToken)
        {
            string url = "https://m.ali213.net/news/gl1910/371835.html";
            var document = await HttpClientHelper.HttpHtml(url, false, cancellationToken);

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
                            GetQA_Item(anwsers, fulls, p);
                        }
                        if (i + 2 <= ps.Count)
                        {
                            var p = ps[i + 2];
                            GetQA_Item(anwsers, fulls, p);
                        }
                    }
                }
            }

            return (anwsers, fulls);
        }

        private void GetQA_Item(List<string> anwsers, List<string> fulls, HtmlNode p)
        {
            fulls.Add(p.InnerText.Trim());

            {
                var span = p.SelectSingleNode("span");
                if (span != null)
                {
                    var strong = span.SelectSingleNode("strong");
                    if (strong != null)
                    {
                        anwsers.Add(strong.InnerText.Trim());
                        return;
                    }
                }
            }

            {
                var font = p.SelectSingleNode("font");
                if (font != null)
                {
                    var b = font.SelectSingleNode("b");
                    if (b != null)
                    {
                        anwsers.Add(b.InnerText.Trim());
                        return;
                    }
                }
            }

            {
                var strong = p.SelectSingleNode("strong");
                if (strong != null)
                {
                    var font = strong.SelectSingleNode("font");
                    if (font != null)
                    {
                        anwsers.Add(font.InnerText.Trim());
                        return;
                    }
                }
            }

            {
                string line = p.InnerText.Trim();

                int lastChineseQuestion = line.LastIndexOf('？');
                int lastEnglishQuestion = line.LastIndexOf('?');

                int lastIndex = Math.Max(lastChineseQuestion, lastEnglishQuestion);

                if (lastIndex >= 0)
                {
                    anwsers.Add(line.Substring(lastIndex + 1));
                    return;
                }
            }

            anwsers.Add(p.InnerText.Trim());
        }
    }

    [ImplementationOrder(3)]
    public class GetQA_xuexili_jinridaan : IGetQA
    {
        public async Task<(List<string> anwsers, List<string> fulls)> GetQA(DateTime date, CancellationToken cancellationToken)
        {
            string url = "http://www.xuexili.com/mayizhuangyuan/jinridaan.html";
            var document = await HttpClientHelper.HttpHtml(url, false, cancellationToken);

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
