using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Data;
using System.Threading;

public class Module
{
    static BitArray FSPEC;
    static DataTable FSPECtable;
    static List<string> ASCIIlist, EmitterCategorylist;

    /// <summary>
    /// Возвращает EmitterCategory таблицу.
    /// </summary>
    /// <param name="data">Строка для обработки.</param>
    /// <returns>Список</returns>
    public static List<string> GetEmitterCategorytable()
    {
        List<string> list = new List<string>();
        string data = Flight_Radar_Module.Properties.Resources.Emitter_Category;
        StringReader dataReader = new StringReader(data);
        string str = "";
        while ((str = dataReader.ReadLine()) != null)
        {
            list.Add(str);
        }
        return list;
    }
    /// <summary>
    /// Декодирует координату. 130 спецификация.
    /// </summary>
    /// <param name="coordinatebytes">Массив байт координаты.</param>
    /// <returns>Координата.</returns>
    static double CoordinateDecoder130(byte[] coordinatebytes)
    {
        return Convert.ToDouble(BitConverter.ToInt32(coordinatebytes.Reverse().ToArray(), 0) * 0.000021457672119140625);
    }
    /// <summary>
    /// Декодирует координату. 131 спецификация.
    /// </summary>
    /// <param name="coordinatebytes">Массив байт координаты.</param>
    /// <returns>Координата.</returns>
    static double CoordinateDecoder131(byte[] coordinatebytes)
    {
        return Convert.ToDouble(BitConverter.ToInt32(coordinatebytes.Reverse().ToArray(), 0) / 5965232.3555555599221118074846386);
    }
    /// <summary>
    /// Декодирует высоту. 140 спецификация.
    /// </summary>
    /// <param name="heightbytes">Массив байт высоты.</param>
    /// <returns>Высота.</returns>
    static double HeightDecoder140(byte[] heightbytes)
    {
        return Convert.ToInt32(BitConverter.ToInt16(heightbytes.Reverse().ToArray(), 0) * 6.25 * 0.3048);
    }
    /// <summary>
    /// Декодирует Mode3A.
    /// </summary>
    /// <param name="Mode3Abytes"></param>
    /// <returns>Mode3A</returns>
    static string Mode3ADecoder070(byte[] Mode3Abytes)
    {
        BitArray bits = new BitArray(Mode3Abytes.Reverse().ToArray());
        BitArray codebits = new BitArray(8);
        string result = "";
        byte[] code = new byte[1];
        int pos = bits.Length -1 - 4;
        while (pos >= 2)
        {
            codebits.SetAll(false);
            for (int bit = 2; bit >= 0; bit--)
            {
                codebits.Set(bit, bits.Get(pos));
                pos--;
            }
            codebits.CopyTo(code, 0);
            result += Convert.ToInt32(code[0]).ToString();
        }
            return result;
    }

    /// <summary>
    /// Инициализация компонентов модуля.
    /// </summary>
    public static void Init()
    {
        FSPECtable = Protocol.GetFSPECtable();
        ASCIIlist = Protocol.GetASCIItable();
        EmitterCategorylist = GetEmitterCategorytable();   
    }

    /// <summary>
    /// Обрабатывает категорию.
    /// </summary>
    /// <param name="ProtocolStream">ASTERIX пакет.</param>
    /// <param name="message">Таблица маршрутных точек.</param>
    public static void Decode(MemoryStream ProtocolStream, DataTable message, int category)
    {
        while (ProtocolStream.Position != ProtocolStream.Length)
        {
            byte[] TargetAddressbytes = new byte[4];
            byte[] TimePosition = new byte[4];
            byte[] Latitudebytes = new byte[4];
            byte[] Longitudebytes = new byte[4];
            byte[] AircraftIdentificationbytes = new byte[6];
            byte[] AirportDepaturebytes = new byte[4];
            byte[] AirportArrivalbytes = new byte[4];
            byte[] Callsing = new byte[7];
            byte[] Heightbytes = new byte[2];
            byte[] Mode3Abytes = new byte[2];

            string TargetAddress = "";
            string AircraftIdentification = "";
            string Latitude = "";
            string Longitude = "";
            string EmitterCategory = "";
            string AirportDepature = "";
            string AirportArrival = "";
            string Height = "-1";
            string SAC = "";
            string SIC = "";
            string Mode3A = "";
            string CAT = Convert.ToString(category);

            FSPEC = Protocol.GetVariableField(ProtocolStream);
            if (FSPEC.Length <= FSPECtable.Rows.Count)
            {
                for (int FSPECbit = 0; FSPECbit < FSPEC.Length; FSPECbit++)
                {
                    if (FSPEC.Get((FSPEC.Length - 1) - FSPECbit) == true)
                    {
                        string di = Convert.ToString(FSPECtable.Rows[FSPECbit]["Data Item"]);

                        switch (di)
                        {
                            case "010":
                                {
                                    if (Protocol.ChekEndPacket(ProtocolStream, 2))
                                    {
                                        SAC = Convert.ToString(ProtocolStream.ReadByte());
                                        SIC = Convert.ToString(ProtocolStream.ReadByte());
                                    }
                                    break;
                                }
                            case "020":
                                {
                                    if (Protocol.ChekEndPacket(ProtocolStream, 1))
                                    {
                                        EmitterCategory = EmitterCategorylist[ProtocolStream.ReadByte()];
                                    }
                                    break;
                                }
                            case "070":
                                {
                                    if (Protocol.ChekEndPacket(ProtocolStream, 2))
                                    {
                                        ProtocolStream.Read(Mode3Abytes, 0, 2);
                                        Mode3A = Mode3ADecoder070(Mode3Abytes);
                                    }
                                    break;
                                }
                            case "073":
                                {
                                    if (Protocol.ChekEndPacket(ProtocolStream, 3))
                                    {
                                        ProtocolStream.Read(TimePosition, 1, 3);
                                    }
                                    break;
                                }
                            case "080":
                                {
                                    if (Protocol.ChekEndPacket(ProtocolStream, 3))
                                    {
                                        ProtocolStream.Read(TargetAddressbytes, 0, 3);

                                        TargetAddress = "";
                                        for (int i = 0; i < 3; i++)
                                        {
                                            if (Convert.ToString(TargetAddressbytes[i], 16).Length < 2)
                                            {
                                                TargetAddress += "0";
                                            }
                                            TargetAddress += Convert.ToString(TargetAddressbytes[i], 16).ToUpper();
                                        }
                                    }
                                    break;
                                }
                            case "130":
                                {
                                    if (Protocol.ChekEndPacket(ProtocolStream, 3))
                                    {
                                        ProtocolStream.Read(Latitudebytes, 1, 3);
                                        Latitude = Convert.ToString(CoordinateDecoder130(Latitudebytes));
                                    }
                                    if (Protocol.ChekEndPacket(ProtocolStream, 3))
                                    {
                                        ProtocolStream.Read(Longitudebytes, 1, 3);
                                        Longitude = Convert.ToString(CoordinateDecoder130(Longitudebytes));
                                    }
                                    break;
                                }
                            case "131":
                                {
                                    if (Protocol.ChekEndPacket(ProtocolStream, 4))
                                    {
                                        ProtocolStream.Read(Latitudebytes, 0, 4);
                                        Latitude = Convert.ToString(CoordinateDecoder131(Latitudebytes));
                                    }
                                    if (Protocol.ChekEndPacket(ProtocolStream, 4))
                                    {
                                        ProtocolStream.Read(Longitudebytes, 0, 4);
                                        Longitude = Convert.ToString(CoordinateDecoder131(Longitudebytes));
                                    }
                                    break;
                                }
                            case "140":
                                {
                                    if (Protocol.ChekEndPacket(ProtocolStream, 2))
                                    {
                                        ProtocolStream.Read(Heightbytes, 0, 2);
                                        Height = Convert.ToString(HeightDecoder140(Heightbytes));
                                    }
                                    break;
                                }
                            case "170":
                                {
                                    if (Protocol.ChekEndPacket(ProtocolStream, 6))
                                    {
                                        ProtocolStream.Read(AircraftIdentificationbytes, 0, 6);
                                        AircraftIdentification = Protocol.ASCIIDecoder(AircraftIdentificationbytes, ASCIIlist);
                                    }
                                    break;
                                }
                            case "295":
                                {
                                    BitArray DataAgesFSPEC = Protocol.GetVariableField(ProtocolStream);
                                    int countoctet = 0;
                                    for (int bit = 0; bit < DataAgesFSPEC.Length; bit++)
                                    {
                                        if (DataAgesFSPEC.Get(bit) == true)
                                        {
                                            countoctet++;
                                        }
                                    }
                                    countoctet -= (DataAgesFSPEC.Length / 8 - 1);
                                    if (Protocol.ChekEndPacket(ProtocolStream, countoctet))
                                    {
                                        ProtocolStream.Seek(countoctet, SeekOrigin.Current);
                                    }
                                    break;
                                }
                            case "RE":
                                {
                                    if (Protocol.ChekEndPacket(ProtocolStream, ProtocolStream.ReadByte() - 1))
                                    {
                                        ProtocolStream.Position -= 1;
                                        ProtocolStream.Seek(ProtocolStream.ReadByte() - 1, SeekOrigin.Current);
                                    }
                                    break;
                                }
                            case "SP":
                                {
                                    if (Protocol.ChekEndPacket(ProtocolStream, ProtocolStream.ReadByte() - 1))
                                    {
                                        ProtocolStream.Position -= 1;
                                        ProtocolStream.Seek(ProtocolStream.ReadByte() - 1, SeekOrigin.Current);
                                    }
                                    break;
                                }
                            default:
                                {
                                    string length = Convert.ToString(FSPECtable.Rows[FSPECbit]["length"]);
                                    switch (length)
                                    {
                                        case "variable":
                                            {
                                                Protocol.GetVariableField(ProtocolStream);
                                                break;
                                            }
                                        case "":
                                            {
                                                break;
                                            }
                                        default:
                                            {
                                                ProtocolStream.Seek(Convert.ToInt32(length), SeekOrigin.Current);
                                                break;
                                            }
                                    }
                                    break;
                                }
                        }
                    }
                }
                if ((TargetAddress != "") && (Latitude != "") && (Longitude != ""))
                {
                    DataRow newMessage = message.NewRow();
                    newMessage["TargetAddress"] = TargetAddress;
                    newMessage["AircraftIdentification"] = AircraftIdentification;
                    newMessage["EmitterCategory"] = EmitterCategory;
                    newMessage["AirportDepature"] = AirportDepature;
                    newMessage["AirportArrival"] = AirportArrival;
                    newMessage["Latitude"] = Latitude;
                    newMessage["Longitude"] = Longitude;
                    newMessage["Height"] = Height;
                    newMessage["DTime"] = Convert.ToDouble(BitConverter.ToInt32(TimePosition.Reverse().ToArray(), 0) / 128);
                    newMessage["SAC"] = SAC;
                    newMessage["SIC"] = SIC;
                    newMessage["Mode3A"] = Mode3A;
                    newMessage["CAT"] = CAT;

                    message.Rows.Add(newMessage);
                }
            }
        }
    }
}
