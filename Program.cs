using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

enum QState
{
    accepted, completed, unknown, failed, reported
}

class GameState
{
    public int Money = 999999;
    public Dictionary<string, bool> NpcWeSpokeTo = new Dictionary<string, bool>();
    public Dictionary<int, bool> GlobalFlags = new Dictionary<int, bool>();
    public Dictionary<int, QState> QuestState = new Dictionary<int, QState>();
    public Dictionary<int, int> Inventory = new Dictionary<int, int>();
    public string GameOver = null;
    public int CurrentLoc;

    //Загружаем сохранение
    public static GameState Load(string path)
    {
        GameState state = new GameState();

        XElement xLoad = XElement.Load(path);
        XElement xNpcWeSpokeTo = xLoad.Element("NpcWeSpokeTo");
        foreach (XElement xNpc in xNpcWeSpokeTo.Elements())
        {
            state.NpcWeSpokeTo.Add(xNpc.Value, false);
        }
        XElement xGlobalFlags = xLoad.Element("GlobalFlags");
        foreach (XElement xFlag in xGlobalFlags.Elements())
        {
            state.GlobalFlags.Add((int)xFlag, false);
        }
        XElement xReceivedQuest = xLoad.Element("ReceivedQuest");
        foreach (XElement xQuest in xReceivedQuest.Elements())
        {
            //    state.QuestState.Add((int)xQuest, false);
        }
        XElement xInventory = xLoad.Element("Inventory");
        foreach (XElement xItem in xInventory.Elements())
        {
            int id = (int)xItem.Attribute("id");
            state.Inventory.Add(id, (int)xItem);
        }

        XElement xMoney = xLoad.Element("Money");
        state.Money = (int)xMoney; 

        return state;
    }
    public void Save(string path)
    {
        XElement xSave = new XElement("save");
        //Сохранем тех, с кем поговорили
        XElement xNpcWeSpokeTo = new XElement("NpcWeSpokeTo");
        xSave.Add(xNpcWeSpokeTo);
        foreach (string npc in NpcWeSpokeTo.Keys)
        {
            XElement xNpc = new XElement("npc", npc);
            xNpcWeSpokeTo.Add(xNpc);
        }

        XElement xGlobalFlags = new XElement("GlobalFlags");
        xSave.Add(xGlobalFlags);
        foreach (int flag in GlobalFlags.Keys)
        {
            XElement xFlag = new XElement("flag", flag);
            xGlobalFlags.Add(xFlag);
        }

        XElement xReceivedQuest = new XElement("ReceivedQuest");
        xSave.Add(xReceivedQuest);
        foreach (int quest in QuestState.Keys)
        {
            XElement xQuest = new XElement("quest", quest);
            xReceivedQuest.Add(xQuest);
        }

        XElement xInventory = new XElement("Inventory");
        xSave.Add(xInventory);
        foreach (int item in Inventory.Keys)
        {
            int q = Inventory[item];
            XElement xItem = new XElement("item", q);
            xInventory.Add(xItem);
            XAttribute xQ = new XAttribute("id", item);
            xItem.Add(xQ);
        }

        XElement xMoney = new XElement("Money", Money);
        xSave.Add(xMoney);

        xSave.Save(path);
    }

}

class Program
{
    static GameState state;
    static string locationPath = @"..\..\Content\Loc";
    static string citymapPath = @"..\..\Content\citymap.xml";
    static string savePath = @"..\..\Save.xml";
    static string itrmsPath = @"..\..\Content\Itrms.xml";
    static string questPath = @"..\..\Content\Quests.xml";
    static string endingsPath = @"..\..\Content\Endings.xml";
    static XElement xEndings;
    static XElement xCityMap;
    static XElement xItems;
    static XElement xListQuest;

    static void Main(string[] args)
    {
        Console.SetWindowSize(80, 25);
        Console.SetBufferSize(80, 25);

        var mp3reader = new MediaFoundationReader(@"..\..\Content\music\3.m4a");
        var waveout = new WaveOutEvent();
        waveout.Init(mp3reader);
        waveout.Play();

        xItems = XElement.Load(itrmsPath);
        xListQuest = XElement.Load(questPath);
        xEndings = XElement.Load(endingsPath);

        while (8 == 8)
        {
            Console.Clear();
            Center("ЛЕТНЯЯ ПРОЕКТНАЯ АКАДЕМИЯ", 2);
            Center("МАСТЕРСКАЯ: ТЕКСТОВОЕ ПРИКЛЮЧЕНИЕ", 4);
            Center("ПРИКЛЮЧЕНИЯ В ДАВНЕГРАДЕ", 6);
            Right("Разработчики:    ", 8);
            Right("Егор Деревягин   ", 9);
            Right("Юрий Фролов      ", 10);
            Right("Виктор Тюмереков ", 11);
            Right("Александр Моисеев", 12);
            Right("Мастер:          ", 13);
            Right("Тимур Вишняков   ", 14);
            Center("Абакан 2019", 24);
            Center("1. НОВАЯ ИГРА", 10);
            Center("2. ПРОДОЛЖИТЬ ИГРУ", 12);
            Center("3. ВЫХОД", 14);

            int e = ReadReplyNumber(3);
            if (e == 0)
            {
                state = new GameState();
                PlayGame();
            }
            else if (e == 1)
            {
                state = GameState.Load(@"..\..\Save.xml");
                PlayGame();
            }
            else
            {
                return;
            }
        }
    }

    static void Center(string s, int line)
    {
        int x = Console.BufferWidth / 2 - s.Length / 2;
        Console.SetCursorPosition(x, line);
        Console.WriteLine(s);
    }

    static void Right(string s, int line)
    {
        int x = Console.BufferWidth - s.Length;
        Console.SetCursorPosition(x, line);
        Console.WriteLine(s);
    }

    static void PlayGame()
    {
        Console.Clear();

        xCityMap = XElement.Load(citymapPath);
        state.CurrentLoc = 1;

        while (state.GameOver == null)
        {
            XElement xLoc = GetLocByID(xCityMap, state.CurrentLoc);

            string text = xLoc.Element("texts").Value.Trim();
            Console.WriteLine("Вы находитесь: {0}.", text);
            foreach (XElement xCharacter in xLoc.Elements("character"))
                Console.WriteLine(xCharacter.Element("textl").Value.Trim());

            Console.WriteLine("1) Уйти");
            Console.WriteLine("2) Подойти к...");
            Console.WriteLine("3) Инвентарь");
            Console.WriteLine("4) Квесты");
            Console.WriteLine("5) Выход в главное меню");
            int r = ReadReplyNumber(5);
            if (r == 0)
            {
                List<XElement> ways = new List<XElement>();
                // вывод всех путей в другие локации
                foreach (XElement xWay in xLoc.Elements("way"))
                {
                    ways.Add(xWay);
                    Console.WriteLine("{0}) {1}", ways.Count, xWay.Value.Trim());
                }
                r = ReadReplyNumber(ways.Count);
                state.CurrentLoc = (int)ways[r].Attribute("to");
            }
            else if (r == 1)
            {
                // вывод всех персонажей в локации
                List<XElement> characters = new List<XElement>();
                foreach (XElement xCharacter in xLoc.Elements("character"))
                {
                    characters.Add(xCharacter);

                    string characterDescription;
                    string link = xCharacter.Attribute("link").Value.Trim();
                    characterDescription = xCharacter.Element("texts").Value.Trim();
                    Console.WriteLine("{0}) {1}", characters.Count, characterDescription);
                }
                r = ReadReplyNumber(characters.Count);
                string characterFile = characters[r].Attribute("link").Value.Trim();
                PlayDialog(LoadCharacter(characterFile));

                if (!state.NpcWeSpokeTo.ContainsKey(characterFile))
                    state.NpcWeSpokeTo.Add(characterFile, false);

                state.Save(savePath);
            }
            else if (r == 2)
            {
                foreach (int itemId in state.Inventory.Keys)
                {
                    int q = state.Inventory[itemId];
                    foreach (XElement xItem in xItems.Elements())
                        if ((int)xItem.Attribute("id") == itemId)
                        {
                            Console.WriteLine("{0} - {1} шт.", (string)xItem, q);
                            break;
                        }
                }
                Console.WriteLine("Монеты - {0} шт.", state.Money);

            }
            else if (r == 3)
            {
                foreach (int QuestId in state.QuestState.Keys)
                {
                    foreach (XElement xQuest in xListQuest.Elements())
                        if ((int)xQuest.Attribute("id") == QuestId)
                        {
                            Console.WriteLine("{0}", (string)xQuest);
                            break;
                        }
                }
            }
            else
            {
                return;
            }
        }

        ShowEnding();
    }

    private static void ShowEnding()
    {
        XElement xEnding = null;
        foreach (XElement xEnding0 in xEndings.Elements())
            if ((string)xEnding0.Attribute("id") == state.GameOver)
            {
                xEnding = xEnding0;
                break;
            }

        foreach (XElement xText in xEnding.Elements())
        {
            if (CheckAttributes(xText))
                Console.WriteLine(xText.Value.Trim());
        }

        System.Threading.Thread.Sleep(5000);
        Console.WriteLine("[нажмите любую клавишу для продолжения]");
        Console.ReadKey();
    }

    private static XElement LoadCharacter(string fileName)
    {
        string[] files =
            System.IO.Directory.GetFiles(locationPath, "*.xml", System.IO.SearchOption.AllDirectories);
        foreach (string file in files)
        {
            if (System.IO.Path.GetFileNameWithoutExtension(file) == fileName)
                return XElement.Load(file);
        }

        return null;
    }

    private static XElement GetLocByID(XElement xCityMap, int id)
    {
        foreach (XElement xLoc in xCityMap.Elements("loc"))
        {
            int _id = (int)xLoc.Attribute("id");
            if (_id == id)
                return xLoc;
        }
        return null;
    }

    static int ReadReplyNumber(int n)
    {
        int choiceN = 0;
        while (8 == 8)
        {
            string choice = Console.ReadLine();
            int.TryParse(choice, out choiceN);
            if (choiceN > 0 && choiceN <= n)
                break;
            else
                Console.WriteLine("Попытайесь ещё раз");
        }

        return choiceN - 1;
    }

    private static XElement ShowDialogNode(XElement xCurrentNode)
    {
        XElement xtext = xCurrentNode.Element("text");
        Console.WriteLine(xtext.Value);
        IEnumerable<XElement> xreplies = xCurrentNode.Elements("reply");
        List<XElement> repliesList = new List<XElement>();

        foreach (XElement xr in xreplies)
        {
            if (CheckAttributes(xr))
            {
                Console.WriteLine("{0}) {1}", repliesList.Count + 1, xr.Value);
                repliesList.Add(xr);
            }
        }

        int choiceN = ReadReplyNumber(repliesList.Count);



        XElement xreply = repliesList[choiceN];
        return xreply;
    }

    private static bool CheckAttributes(XElement xReply)
    {
        int? price = (int?)xReply.Attribute("price"); // сколько нужно денег, чтобы показать реплику
        int? price_ = (int?)xReply.Attribute("price_"); // сколько должно не быть денег, чтобы показать реплику
        int? flagset = (int?)xReply.Attribute("flagset");// проверка установлен ли флаг
        int? flagnotset = (int?)xReply.Attribute("flagnotset");// проверка не установлен ли флаг
        int? quest = (int?)xReply.Attribute("quest");// номер квеста

        XAttribute xCheckquest = xReply.Attribute("checkquest");
        QState checkquest = xCheckquest != null
            ? (QState)Enum.Parse(typeof(QState), xCheckquest.Value)
            : QState.unknown;

        int? item = (int?)xReply.Attribute("item");
        int? noitem = (int?)xReply.Attribute("noitem");

        QState questState =
            quest != null && state.QuestState.ContainsKey(quest.Value)
            ? state.QuestState[quest.Value]
            : QState.unknown;

        bool _price = (price == null && price_ == null)
                || (price != null && price.Value <= state.Money)
                || (price_ != null && price_.Value > state.Money);

        bool _flagset = flagset == null || state.GlobalFlags.ContainsKey(flagset.Value);
        bool _flagnotset = flagnotset == null || !state.GlobalFlags.ContainsKey(flagnotset.Value);
        bool _quest = quest == null || xCheckquest == null || questState == checkquest;
        bool _item = item == null || state.Inventory.ContainsKey(item.Value);
        bool _noitem = noitem == null || !state.Inventory.ContainsKey(noitem.Value);

        return _price && _flagset && _flagnotset && _quest && _item && _noitem;

    }

    /// <summary>
    /// Проигрывыет диалог с неигровым персонажем
    /// </summary>
    /// <param name="xDialog">xml данные для диалога</param>
    private static void PlayDialog(XElement xDialog)
    {
        XElement xCurrentNode = xDialog.Element("node");

        while (8 == 8)
        {
            XElement xreply = ShowDialogNode(xCurrentNode);
                        
            int? flag = (int?)xreply.Attribute("flag");
            if (flag != null)
            {
                if (!state.GlobalFlags.ContainsKey(flag.Value))
                    state.GlobalFlags.Add(flag.Value, false);
            }

            int? quest = (int?)xreply.Attribute("quest");
            XAttribute xSetquest = xreply.Attribute("setquest");
            QState setquest = xSetquest != null
                ? (QState)Enum.Parse(typeof(QState), xSetquest.Value)
                : QState.unknown;

            if (quest != null && xSetquest != null)
            {
                if (state.QuestState.ContainsKey(quest.Value))
                    state.QuestState[quest.Value] = setquest;
                else
                    state.QuestState.Add(quest.Value, setquest);
            }

            int? givemoney = (int?)xreply.Attribute("givemoney");//выдача денег
            if (givemoney != null)
            {
                state.Money += givemoney.Value;
            }

            state.GameOver = (string)xreply.Attribute("gameover");// game over
            if (state.GameOver != null) return;

            int? teleport = (int?)xreply.Attribute("teleport");
            if (teleport != null)
            {
                state.CurrentLoc = teleport.Value;
            }

            int? take = (int?)xreply.Attribute("take");
            if (take != null)
            {
                if (state.Inventory.ContainsKey(take.Value))
                    state.Inventory[take.Value] += 1;
                else
                    state.Inventory.Add(take.Value, 1);
            }

            int? give = (int?)xreply.Attribute("give");
            if (give != null)
            {
                if (state.Inventory.ContainsKey(give.Value))
                {
                    int q = state.Inventory[give.Value];
                    if (q == 1)
                        state.Inventory.Remove(give.Value);
                    else
                        state.Inventory[give.Value] = q - 1;
                }

                else
                    throw new InvalidOperationException();
            }


            XAttribute xgoto = xreply.Attribute("goto");
            if (xgoto == null) return;

            string gotoId = xgoto.Value;
            foreach (XElement xnode in xDialog.Elements())
            {
                if (xnode.Attribute("id").Value == gotoId)
                {
                    xCurrentNode = xnode;
                    break;
                }
            }
        }

    }
}
