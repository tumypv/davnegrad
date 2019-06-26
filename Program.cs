//using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

enum QState
{
    accepted, completed, unknown, failed, reported
}

class GameState
{
    public int Money = 0;
    public Dictionary<string, bool> NpcWeSpokeTo = new Dictionary<string, bool>();
    public Dictionary<int, bool> GlobalFlags = new Dictionary<int, bool>();
    public Dictionary<int, QState> QuestState = new Dictionary<int, QState>();
    public Dictionary<int, int> Inventory = new Dictionary<int, int>();
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

        xSave.Save(path);
    }
    
}

class Program
{
    static GameState state = new GameState();
    static string locationPath = @"..\..\Content\Loc";
    static string citymapPath = @"..\..\Content\citymap.xml";
    static string savePath = @"..\..\Save.xml";
    static XElement xCityMap;

    static void Main(string[] args)
    {
        //var mp3Reader = new Mp3FileReader(@"C:\LPA2019\taverna.mp3");
        //var waveOut = new WaveOutEvent();
        //waveOut.Init(mp3Reader);
        //waveOut.Play();
        //state = GameState.Load(@"..\..\Save.xml");

        xCityMap = XElement.Load(citymapPath);
        state.CurrentLoc = 1;

        while (8 == 8)
        {
            XElement xLoc = GetLocByID(xCityMap, state.CurrentLoc);

            string text = xLoc.Element("texts").Value.Trim();
            Console.WriteLine("Вы находитесь: {0}.", text);
            Console.WriteLine("1) Уйти");
            Console.WriteLine("2) Осмотреться");
            int r = ReadReplyNumber(2);
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
            else
            {
                // вывод всех персонажей в локации
                List<XElement> characters = new List<XElement>();
                foreach (XElement xCharacter in xLoc.Elements("character"))
                {
                    characters.Add(xCharacter);

                    string characterDescription;
                    string link = xCharacter.Attribute("link").Value.Trim();
                    if (state.NpcWeSpokeTo.ContainsKey(link))
                        characterDescription = xCharacter.Element("texts").Value.Trim();
                    else
                        characterDescription = xCharacter.Element("textl").Value.Trim();
                    Console.WriteLine("{0}) {1}", characters.Count, characterDescription);
                }
                r = ReadReplyNumber(characters.Count);
                string characterFile = characters[r].Attribute("link").Value.Trim();
                PlayDialog(LoadCharacter(characterFile));

                if (!state.NpcWeSpokeTo.ContainsKey(characterFile))
                    state.NpcWeSpokeTo.Add(characterFile, false);

                state.Save(savePath);
            }
        }
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

    private static List<XElement> ShowDialogNode(XElement xCurrentNode)
    {
        XElement xtext = xCurrentNode.Element("text");
        Console.WriteLine(xtext.Value);
        IEnumerable<XElement> xreplies = xCurrentNode.Elements("reply");
        List<XElement> repliesList = new List<XElement>();
        foreach (XElement xr in xreplies)
        {
            int? price = (int?)xr.Attribute("price"); // сколько нужно денег, чтобы показать реплику
            int? price_ = (int?)xr.Attribute("price_"); // сколько должно не быть денег, чтобы показать реплику
            int? flagset = (int?)xr.Attribute("flagset");
            int? flagnotset = (int?)xr.Attribute("flagnotset");
            int? quest = (int?)xr.Attribute("quest");

            XAttribute xCheckquest = xr.Attribute("checkquest");
            QState checkquest = xCheckquest != null
                ? (QState)Enum.Parse(typeof(QState), xCheckquest.Value)
                : QState.unknown;

            int? item = (int?)xr.Attribute("item");
            int? noitem = (int?)xr.Attribute("noitem");

            if (
                (
                    (price == null && price_ == null)
                    || (price != null && price.Value <= state.Money)
                    || (price_ != null && price_.Value > state.Money)
                )
                &&
                (
                    flagset == null || state.GlobalFlags.ContainsKey(flagset.Value)
                )
                &&
                (
                    flagnotset == null || !state.GlobalFlags.ContainsKey(flagnotset.Value)
                )
                &&
                (
                    quest == null || xCheckquest == null || (state.QuestState.ContainsKey(quest.Value) && state.QuestState[quest.Value] == checkquest)
                )
                &&
                (
                    item == null || state.Inventory.ContainsKey(item.Value)
                )
                &&
                (
                    noitem == null || !state.Inventory.ContainsKey(item.Value)
                )
            )
            {
                Console.WriteLine("{0}) {1}", repliesList.Count + 1, xr.Value);
                repliesList.Add(xr);
            }
        }

        return repliesList;
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
            List<XElement> repliesList = ShowDialogNode(xCurrentNode);
            int choiceN = ReadReplyNumber(repliesList.Count);
            XElement xreply = repliesList[choiceN];
            int? flag = (int?)xreply.Attribute("flag");
            if (flag != null)
                state.GlobalFlags.Add(flag.Value, false);

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
