using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace apk_CFG
{
    public class method
    {
        public string methodName;//方法名称： dowhileCirculate()V ，<init>()V
        public string storeMethodName;//改成合法文件名的方法名称     
        public List<int> borderIndex;//代码块的起始下标
        public List<string> InstruBlock;//代码指令块
        public string locals;//.locals 区域的信息 num|hang
        public string begendInfo;// method块的 开始和结束的行号 sourpath|beg|end|[end]多个end
        public List<int> keyWordHang;//记录if，goto语句的行号

        public List<string> LinkFunc;//起点|终点|显示信息  -- 显示信息为single时，不进行任何连接操作
        public List<string> LinkHead;//代码块头部信息
        public List<string> LinkTail;//代码块尾部信息

        public string methodContent;//method 内容
        public string xmlPath;//导出的xml文件的绝对路径
        public string SourceSmaliPath;
        public int startH;
        public string[] keyWord = { "^goto", "^if-", "^invoke-" ,"^\\.catch", // "."代表除\n外的所有字符，所以必须转义
                                      "^packed-switch ","^sparse-switch ",
                                      "^\\.end packed-switch", "^\\.end sparse-switch","^:","^return"};
        //sourceSmaliPath: 待分析的源smali文件的位置，即类的位置
        //startHang:在源文件中的起始行数
        public method(string strMethod,string xmlPath,string sourceSmaliPath, int startHang)
        {
            this.methodContent = strMethod;
            this.xmlPath = xmlPath;// D:\Documents\GitHub\apk-CFG\apk_CFG\bin\Debug\smaliFile\[MainActivity]
            this.SourceSmaliPath = sourceSmaliPath;
            this.startH = startHang;
            InstruBlock = new List<string>();
            borderIndex = new List<int>();
            LinkFunc = new List<string>();
            LinkHead = new List<string>();
            LinkTail = new List<string>();
            keyWordHang = new List<int>();

            anaLocalBegEndInfo();
            anaName();
            clearBlank();
            storeBlock();
            justLink();
            ExportXML.exportXML(xmlPath, storeMethodName, InstruBlock, LinkFunc, begendInfo,locals);
            //exportXML();
        }

        ~method()
        { }

        //分析method中source,locals ，.prologue, return-的行号
        public void anaLocalBegEndInfo()
        {
            int index=0, end=0;
            int st=0,lo;
            List<int> ter = new List<int>();
            int count=0;
            string toJudge;
            //Regex r = new Regex("^    goto", RegexOptions.Compiled); ;
            while (end != -1 && index < methodContent.Length)
            {
                end = methodContent.IndexOf("\n", index);
                toJudge = methodContent.Substring(index, end - index);
                if (toJudge.IndexOf(".locals") != -1)//将locals信息记录
                {
                    string[] localsub = toJudge.Split(' ');
                    lo = (this.startH + count);
                    locals = localsub[localsub.Length - 1] + "|"+lo ;
                }
                else if (toJudge.IndexOf(".prologue") != -1)//将.prologue信息记录，即开始行号
                    st = this.startH + count;
                else if (toJudge.IndexOf("return") != -1)//结束的行号
                    ter.Add(this.startH + count);
                else if (toJudge.IndexOf("if-") != -1 || toJudge.IndexOf(" goto") !=-1)//匹配if和goto关键字行号,r.IsMatch(toJudge)
                    keyWordHang.Add(this.startH + count);
                count++;
                index = end + 1;
            }
            begendInfo = this.SourceSmaliPath + "|" + st;//source ,beg ,end info
            foreach (int tertmp in ter)
                begendInfo += ("|" + tertmp);
            
        }

        //分析本method的名称
        //将method中的 <>符号改成 ￥, \\ 改成_
        public void anaName()
        {
            int index, end;
            end = this.methodContent.IndexOf("\n");
            index = this.methodContent.LastIndexOf(" ", end);//LastIndexOf从开始的索引，逆向搜索
            storeMethodName = this.methodName = this.methodContent.Substring(index + 1, end - index - 1);
            this.storeMethodName = this.storeMethodName.Replace("<", "_");
            this.storeMethodName = this.storeMethodName.Replace(">", "_");
            this.storeMethodName = this.storeMethodName.Replace("/", "_");
        }
        
        //去除method中多余的空格
        public void clearBlank()
        {
            int index = 0;
            while (index <= this.methodContent.Length - 1)
            {
                string tmp = getNextLine(index);
                if (tmp == "\r\n")//----------或者\n
                    this.methodContent = this.methodContent.Remove(index, 2);
                else if(tmp == "\n")
                    this.methodContent = this.methodContent.Remove(index, 1);
                else if (tmp[0] == ' ')
                {
                    this.methodContent = this.methodContent.Remove(index, 4);
                    index += (tmp.Length - 4);
                }
                else
                    index += tmp.Length;
            }
            
        }

        //将method分块
        public void methodSplit()
        {
            int index=0,preIndex = index;//preIndex 记录前一行的索引
            string tmp = getNextLine(index);
            List<int> border_tmp = new List<int>();
            Regex r;
            int i;
            while (index <= this.methodContent.Length - 1)
            {
                for (i = 0; i < keyWord.Length - 2; i++ )
                {
                    r = new Regex(keyWord[i], RegexOptions.Compiled);
                    if (r.IsMatch(tmp))
                    {
                        border_tmp.Add(index + tmp.Length);
                        break;
                    }                        
                }

                //判断 : 标号
                r = new Regex(keyWord[keyWord.Length-2], RegexOptions.Compiled);                    
                if (r.IsMatch(tmp))
                {
                    string maybeLine = getNextLine(preIndex);
                    preIndex = maybeLine.IndexOf(".line") == -1 ? index : preIndex;//对上面一行是否为.line进行判断
                    border_tmp.Add(preIndex);
                }                        
                preIndex = index;
                index += tmp.Length;
                tmp = getNextLine(index);
            }
            this.borderIndex = border_tmp.Distinct().ToList();
        }

        //将分块存储进去
        //【测试】正式使用修改
        public void storeBlock()
        {
            methodSplit();
            
            if (borderIndex.Count == 0)//只有一个代码块的method，直接返回整个内容
            {
                InstruBlock.Add(this.methodContent);
                return;
            }
            int index = 0;
            string blockTmp;
            int indexOfhang=0;//行号数组的下标记录
            //Regex r = new Regex("^goto", RegexOptions.Compiled);
            foreach (int in_tmp in borderIndex)
            {
                blockTmp = this.methodContent.Substring(index, in_tmp - index);
                if (blockTmp.IndexOf("if-") != -1 || blockTmp.IndexOf("\ngoto") != -1)//如果代码块中有goto或者if，那么加上对应的行号
                    blockTmp += ("|" + keyWordHang[indexOfhang++]);
                InstruBlock.Add(blockTmp);
                index = in_tmp;
            }
            //如果最后一个分界点不是字符串尾部
            if (this.methodContent.Length != borderIndex[borderIndex.Count - 1]) 
                InstruBlock.Add(this.methodContent.Substring(borderIndex[borderIndex.Count - 1], this.methodContent.Length - borderIndex[borderIndex.Count - 1]));
            string finalBlock = InstruBlock[ InstruBlock.Count -1];
            //为最后一个代码块添加 终止标示符，统一格式，方便检索
            if (finalBlock[finalBlock.Length - 1] != '\n')
            {
                InstruBlock.RemoveAt(InstruBlock.Count -1);
                InstruBlock.Add(finalBlock + "\n");//---------正式使用修改
            }
        }

              
        //分析method中的结构，并写入 结点连接数组
        public void justLink()
        {
            gartherHeadTail();
            if (InstruBlock.Count == 1)//只有一个代码块的method
            {
                LinkFunc.Add("-1|-1|single");//不进行任何连接操作
                return;                
            }
            string[] block;
            int i,keySearch,k;
            string Tail;
            List<int> caseDefau = new List<int>();//default case 的跳转对象
            for (i = 0; i < LinkTail.Count; i++)
            {
                Tail = LinkTail[i];
                Regex r ;
                //关键字匹配
                for (keySearch = 0; keySearch < keyWord.Length; keySearch++)
                {
                    r = new Regex(keyWord[keySearch], RegexOptions.Compiled);
                    if (r.IsMatch(Tail)) break;
                }
                //本代码块为return，不做任何链接
                if (keySearch == keyWord.Length - 1) continue;
                //[不一定是有的]  下一个代码块有:标示符。   对于没有考虑的情况，都采取不作为的处理
                if (keySearch == keyWord.Length && (i+1)<=LinkHead.Count-1)
                { 
                    k= i+1;
                    LinkFunc.Add(i + "|" + k + "|" + "cont");
                }
                else if (keySearch == 0)//解析goto指令
                {
                    int jmpTo = parseGoto(Tail);
                    if (jmpTo == -1) continue;//goto语句错误
                    LinkFunc.Add(i + "|" + jmpTo + "|" + "jmp");
                }
                else if (keySearch == 1)//解析if指令
                {
                    int toTRUE = parseIf(Tail);
                    if (toTRUE == -1) continue;//if跳转错误
                    LinkFunc.Add(i + "|" + toTRUE + "|" + "True");
                    k = i+1;
                    LinkFunc.Add(i + "|" + k + "|" + "False");
                }
                else if (keySearch == 2)//解析invoke指令
                {
                    k = i + 1;
                    LinkFunc.Add(i + "|" + k + "|" + "OnReturn");
                }
                else if (keySearch == 3)//解析try/catch指令
                {
                    int[] tc = parseTry(Tail);
                    LinkFunc.Add(i + "|" + tc[0] + "|" + "try");
                    LinkFunc.Add(i + "|" + tc[1] + "|" + "catch");
                }
                else if (keySearch == 4 || keySearch == 5)//解析packed_switch和sparse-switch, 
                {
                    int switchJmp = parseSwitchJmp(Tail);
                    LinkFunc.Add(i + "|" + switchJmp + "|" + "switch");
                    caseDefau.Add(i+1);//加入default case 跳转的索引位置
                }
                else if (keySearch == 6)//解析Tail->.end packed-switch ，
                {
                    //----List传入可以改变数据,ok
                    //----int 转换的测试,ok
                    block = InstruBlock[i].Split('|');
                    List<string> caseName = parseSwitchCase(block[0], i, caseDefau);
                    foreach (string incase in caseName)
                        LinkFunc.Add(incase);
                }
                else if (keySearch == 7)//解析.end sparse-switch
                {
                    block = InstruBlock[i].Split('|');
                    List<string> caseName = parseSwitchSparseCase(block[0], i, caseDefau);
                    foreach (string incase in caseName)
                        LinkFunc.Add(incase);
                }

            }
        }

        //解析goto指令
        public int parseGoto(string gotoCode)
        {
            string[] gotoSub = gotoCode.Split(' ');
            return LinkHead.IndexOf(gotoSub[1]);
        }

        //解析if指令
        public int parseIf(string inCode)
        {
            string[] IFsub = inCode.Split(' ');
            return LinkHead.IndexOf(IFsub[IFsub.Length-1]);
        }
        
        //解析invoke指令--待拓展
        public void parseInvoke()
        { 
            
        }

        //解析try/catch指令--   
        //[存在try 跳转方向的疑问。这里将try指令 指向了try_start的地方]
        //----[存在throw的情况没有分析，  有的throw有catch，  有的没有catch]
        //[测试]，正式修改
        public int[] parseTry(string inTry)
        {
            string[] trySub = inTry.Split(' ');
            int catIndex = LinkHead.IndexOf(trySub[trySub.Length - 1]);
            string tmp = trySub[trySub.Length - 4];
            string trystr = ":try_start_" + tmp[tmp.Length - 1] + "\n";//--正式待修改
            int tryIndex = LinkHead.IndexOf(trystr);
            int[] reint = { tryIndex, catIndex };
            return reint;
        }

        //解析switch跳转指令
        public int parseSwitchJmp(string swit)
        {
            string[] switJmp = swit.Split(' ');
            return LinkHead.IndexOf(switJmp[switJmp.Length - 1]);
        }

        //解析switch-case分发指令
        //1、按照switch分支进行case跳转，  2、default的跳转在 尾部是packed-switch 的按照序号的下一个代码块 
        //返回的形式即为： 起始|终点|case N
        //！！！！比较危险的处理方法，去除代码的前面四个空格。
        public List<string> parseSwitchCase(string switBlock,int currentPoi,List<int> caseDefau)
        {
            int begCaseIndex = switBlock.IndexOf(".packed-switch");
            int endCaseIndex = switBlock.IndexOf("\n", begCaseIndex);
            string[] begCaseSub = switBlock.Substring(begCaseIndex, endCaseIndex - begCaseIndex).Split(' ');
            //begCaseSub[1] = begCaseSub[1].Remove(begCaseSub[1].Length - 1);
            int maybeNeg = 1;
            if (begCaseSub[1].ToString()[0] == '-')//如果十六进制的第一个是 - 号，则去掉负号进行处理
            {
                maybeNeg = -1;
                begCaseSub[1] = begCaseSub[1].Remove(0, 1);
            }
            int begNum = Convert.ToInt32(begCaseSub[1], 16) * maybeNeg;//获取case情况的起始数字
            List<string> caseName = new List<string>();
            string caseTmp = "";
            int index,endIndex = endCaseIndex+1;
            index = endIndex;
            endIndex = switBlock.IndexOf("\n", index) + 1;
            caseTmp = switBlock.Substring(index, endIndex-index);
            while (caseTmp.IndexOf(".end packed-switch") == -1)//当没有到达switch分析的尾部的时候
            {
                //！！！！比较危险的处理方法，去除代码的前面四个空格。
                caseTmp = caseTmp.Remove(0, 4);
                caseName.Add(currentPoi + "|" + LinkHead.IndexOf(caseTmp) + "|" + "case "+begNum);
                begNum++;
                index = endIndex;
                endIndex = switBlock.IndexOf("\n", index) + 1;
                caseTmp = switBlock.Substring(index, endIndex - index);
            }
            //default 情况录入           
            caseName.Add(currentPoi + "|" + caseDefau[caseDefau.Count - 1] + "|" + "default");
            caseDefau.RemoveAt(caseDefau.Count - 1);
            return caseName;
        }

        //解析switch-case sparse分发指令
        //!!!危险操作，移除代码前面四个空格
        public List<string> parseSwitchSparseCase(string switBlock, int currentPoi , List<int> caseDefau)
        {
            //跳过两条代码
            int index = switBlock.IndexOf("\n") + 1;
            int endindex = switBlock.IndexOf("\n", index)+1;
            while (switBlock[index] != ' ')
            {
                index = endindex;
                endindex = switBlock.IndexOf("\n", index) + 1;
            }
            

            string caseTmp = switBlock.Substring(index, endindex - index);
            int caseNum;
            List<string> caseName = new List<string>();
            string[] caseTmpsub;
            while (caseTmp.IndexOf(".end sparse-switch") == -1)
            {
                caseTmp = caseTmp.Remove(0, 4);
                caseTmpsub = caseTmp.Split(' ');
                int maybeNeg = 1;
                if (caseTmpsub[0].ToString()[0] == '-')//如果十六进制的第一个是 - 号，则去掉负号进行处理
                {
                    maybeNeg = -1;
                    caseTmpsub[0] = caseTmpsub[0].Remove(0, 1);
                }
                caseNum = Convert.ToInt32(caseTmpsub[0], 16) * maybeNeg;//test!!!
                caseName.Add(currentPoi + "|" + LinkHead.IndexOf(caseTmpsub[caseTmpsub.Length - 1]) + "|case " + caseNum);
                index = endindex;
                endindex = switBlock.IndexOf("\n", index)+1;
                caseTmp = switBlock.Substring(index, endindex - index);
            }
            //default input
            caseName.Add(currentPoi + "|" + caseDefau[caseDefau.Count - 1] + "|" + "default");
            caseDefau.RemoveAt(caseDefau.Count - 1);
            return caseName;
        }

        //收集每个代码块的头部和尾部信息
        public void gartherHeadTail()
        {
            int i;
            string[] blocksub;
            for (i = 0; i < this.InstruBlock.Count; i++)
            {
                blocksub = InstruBlock[i].Split('|');
                LinkTail.Add(getBlockLastCode(blocksub[0]));
                LinkHead.Add(getBlockHeadCode(blocksub[0]));
            }
        }

        //获取每一行代码
        public string getNextLine(int startIndex)
        {
            int end = this.methodContent.IndexOf("\n", startIndex);
            end = end != -1 ? end+1 : this.methodContent.Length;//对检索到文件尾的处理
            return this.methodContent.Substring(startIndex, end - startIndex);
        }
    
        //获取代码块最后一行代码
        //【测试】  正式读取修改
        public string getBlockLastCode(string input)
        {
            int index = input.Length - 3,end;
            index = input.LastIndexOf("\n", index);
            if (index == -1) return input;//如果只有一行代码则返回自己本身
            string strTmp = input.Substring(index + 1, input.Length - index - 1);

            while (strTmp.IndexOf(".line") != -1 || strTmp.IndexOf(".end local") != -1)//当前行为.line，需要向上多读取一行
            {
                if (index == 0)//如果整个代码块都是无效代码，则任意赋予一个值
                {
                    strTmp = ".line\n";
                    break;
                }
                end = index+1;
                index = input.LastIndexOf("\n", index - 2)+1;
                strTmp = input.Substring(index, end - index);
            }
                
            if (strTmp[strTmp.Length - 1] != '\n') strTmp += "\n";//对最后一行代码的处理--正式修改                
            return strTmp;
        }
    
        //获取代码块头部有效代码
        public string getBlockHeadCode(string input)
        {
            int index = 0,end;
            end = input.IndexOf("\n")+1;
            end = end != -1 ? end : input.Length;//当分析的代码在最后一个的时候，返回这个处理
            string head = input.Substring(index, end - index);
            if (head.IndexOf(".line") == -1) return head;//如果不是.line，即有效行，则返回获取的值
            //反之，当前为.line，需要向下面多读取一行
            index = end;
            end = input.IndexOf("\n", end) + 1;
            head = input.Substring(index, end - index);
            return head;
        }
    }
}
