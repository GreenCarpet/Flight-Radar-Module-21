using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Xml;
using System.Data;

class Protocol
{
    /// <summary>
    /// Возвращает поле переменной длины. Собирает байты, пока последний бит байта не установится в 0.
    /// </summary>
    /// <param name="stream">Поток данных</param>
    /// <returns></returns>
    public static BitArray GetVariableField(Stream stream)
    {
        List<byte> bytes = new List<byte>();
        do
        {
            if (ChekEndPacket(stream, 1))
            {
                bytes.Add(Convert.ToByte(stream.ReadByte()));
            }
            else
            {
                break;
            }
        }
        while (new BitArray(BitConverter.GetBytes(bytes.Last())).Get(0) == true);
        bytes.Reverse();
        return (new BitArray(bytes.ToArray()));
    }
    /// <summary>
    /// Возвращает FSPEC таблицу.
    /// </summary>
    /// <param name="category">Категория.</param>
    /// <returns>FSPEC таблица</returns>
    public static DataTable GetFSPECtable()
    {
        DataTable FSPECtable = new DataTable();
        FSPECtable.Columns.Add("Data Item", System.Type.GetType("System.String"));
        FSPECtable.Columns.Add("Length", System.Type.GetType("System.String"));

        XmlDocument doc = new XmlDocument();
        doc.InnerXml = Flight_Radar_Module.Properties.Resources.FSPEC;

        XmlNodeList FRNList = doc.DocumentElement.ChildNodes;
        for (int frn = 0; frn < FRNList.Count; frn++)
        {
            string FRN = FRNList[frn].Attributes["value"].InnerText;
            if (FRN == "FX")
            {
                FSPECtable.Rows.Add(new object[] { "FX", "" });
            }
            else
            {
                if (FRNList[frn].Attributes.Count == 3)
                {
                    string Data_Item = FRNList[frn].Attributes["Data_Item"].InnerText;
                    string Length = FRNList[frn].Attributes["Length"].InnerText;

                    FSPECtable.Rows.Add(new object[] { Data_Item, Length });
                }
                else
                {
                    FSPECtable.Rows.Add(new object[] { "", "" });
                }
            }

        }

        return FSPECtable;
    }
    /// <summary>
    /// Возвращает ASCII таблицу.
    /// </summary>
    /// <param name="data">Строка для обработки.</param>
    /// <returns>Список</returns>
    public static List<string> GetASCIItable()
    {
        List<string> list = new List<string>();
        string data = Flight_Radar_Module.Properties.Resources.ASCII;
        StringReader dataReader = new StringReader(data);
        string str = "";
        while ((str = dataReader.ReadLine()) != null)
        {
            list.Add(str);
        }
        return list;
    }
    /// <summary>
    /// Декодирует 5 битный ASCII код.
    /// </summary>
    /// <param name="codebytes">Декодируемый массив байт.</param>
    /// <param name="ASCIItable">Таблица ASCII символов.</param>
    /// <returns>Декодированная строка.</returns>
    public static string ASCIIDecoder(byte[] codebytes, List<string> ASCIItable)
    {
        BitArray bits = new BitArray(codebytes.Reverse().ToArray());
        BitArray codebits = new BitArray(8);
        string result = "";
        byte[] code = new byte[1];
        int pos = bits.Length - 1;
        while (pos > 5)
        {
            codebits.SetAll(false);
            for (int bit = 5; bit >= 0; bit--)
            {
                codebits.Set(bit, bits.Get(pos));
                pos--;
            }
            codebits.CopyTo(code, 0);
            result += ASCIItable[Convert.ToInt32(code[0])];
        }
        return result;
    }
    /// <summary>
    /// Проверяет возможность чтения потока данных.
    /// </summary>
    /// <param name="stream">Поток данных.</param>
    /// <param name="offset">Количество считываемых байт.</param>
    /// <returns>Возможность чтения потока данных.</returns>
    public static bool ChekEndPacket(Stream stream, int offset)
    {
        if (stream.Position + offset > stream.Length)
        {
            stream.Position = stream.Length;
            return false;
        }
        return true;
    }
}
