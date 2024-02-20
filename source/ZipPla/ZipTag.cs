using Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public struct SerializableRGB
    {
        public byte R, G, B;

        public static implicit operator Color(SerializableRGB sc)
        {
            return Color.FromArgb(sc.R, sc.G, sc.B);
        }
        public static implicit operator SerializableRGB(Color c)
        {
            return new SerializableRGB { R = c.R, G = c.G, B = c.B };
        }
    }
    
    public class ZipTag
    {
        private string name;
        private SerializableRGB backColor = Color.White;
        private Color foreColor = Color.Black;

        public ZipTag() { }
        public ZipTag(DataGridViewCell cell)
        {
            Name = cell.Value as string;
            BackColor = cell.Style.BackColor;
        }
        public ZipTag(string name)
        {
            Name = name;
            BackColor = GetDefaultBackColor(name);
        }

        public ZipTag(string name, Color backColor)
        {
            Name = name;
            BackColor = backColor;
        }

        public static Color GetDefaultBackColor(string name)
        {
            var random = new Random(name.GetHashCode());
            
            return Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));

            //return Color.FromArgb(128 + random.Next(128), 128 + random.Next(128), 128 + random.Next(128));

            // 幾何的な議論で、これは青色にかたよる傾向になることが分かる
            /*
            for(uint i = 0; i < uint.MaxValue; i++)
            {
                var color = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
                if (nearToWhite(color)) return color;
            }
            return Color.White;
            */
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public SerializableRGB BackColor
        {
            get
            {
                return backColor;
            }
            set
            {
                backColor = value;
                foreColor = GetForeColor(value);
            }
        }

        public Color ForeColor
        {
            get
            {
                return foreColor;
            }
        }

        public static Color GetForeColor(Color backColor)
        {
            //return Color.FromArgb(backColor.R > 128 ? 0 : 255, backColor.G > 128 ? 0 : 255, backColor.B > 128 ? 0 : 255);

            return NearToWhite(backColor) ? Color.Black : Color.White;
        }

        private static double getGrayScale(Color color)
        {
            return 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
        }

        private static double getLInfinityGrayScale(Color color)
        {
            return Math.Max(Math.Max(0.299 * color.R, 0.587 * color.G), 0.114 * color.B) / 0.587;
        }

        private static double getGeometricGrayScale(Color color)
        {
            return (0.299*0.299) * color.R* color.R + (0.587* 0.587) * color.G * color.G + (0.114* 0.114) * color.B * color.B;
        }

        private static double getConsideringGammaGrayScale(Color color)
        {
            return Math.Pow(0.299 * color.R, 2.2) + Math.Pow(0.587 * color.G, 2.2) + Math.Pow(0.114 * color.B, 2.2);
        }

        private static double getLInfinityConsideringGammaGrayScale(Color color)
        {
            return Math.Max(Math.Max((0.299 / 0.587) * Math.Pow(color.R / 255.0, 2.2),
               (0.587 / 0.587) * Math.Pow(color.G / 255.0, 2.2)), (0.114 / 0.587) * Math.Pow(color.B / 255.0, 2.2));

        }

        private static double getConsideringGammaGeometricGrayScale(Color color)
        {
            return Math.Pow(0.299 * color.R, 2.2 * 2) + Math.Pow(0.587 * color.G, 2.2 * 2) + Math.Pow(0.114 * color.B, 2.2 * 2);
        }

        //private static readonly ColorLabComparator whilteComparator = new ColorLabComparator(Color.White, Color.Black);
       // private static readonly ColorLab labWhite = new ColorLab(Color.White);
       // private static readonly ColorLab labBlack = new ColorLab(Color.Black);
        public static bool NearToWhite(Color color)
        {
            //return getGrayScale(color) >= 186.083713474; // 255 * 0.5^(1/2.2)
            //return getGrayScale(color) >= 160; // 灰色系のタグが半々になるように
            //return getGeometricGrayScale(color) >= 0.299 * 0.299 * 255 * 255; // 赤がギリギリ白扱いになる
            //return getConsideringGammaGrayScale(color) >= 0.36 * 76498.1062232; // 白色 = 76498.1062232

            //return getConsideringGammaGeometricGrayScale(color) >= 0.0473661427 * 3916124692.95; // 白色 = 3916124692.95、0.5^(2.2*2) = 0.047...

            //return whilteComparator.NearerToFirst(color);

            return getLInfinityConsideringGammaGrayScale(color) > 0.5;

            //var colorLab = new ColorLab(color);
            //return 2 * labWhite.CIE2000(colorLab) < labBlack.CIE2000(colorLab);
        }

        /*
        public static Color GetSelectionForeColor(Color backColor, Color selectionBackColor)
        {

        }
        */

    }
    
    public class ZipTagConfig : Configuration
    {
        public SerializableRGB ProfileColor = SystemColors.Menu;

        // Message が GeneralConfig を呼ぶので Configration のコンストラクタで Message を呼んではいけない
        //public ZipTag[] Tags = new ZipTag[] { new ZipTag(Message.TagSample1, Color.Pink), new ZipTag(Message.TagSample2, Color.LightGreen), new ZipTag(Message.TagSample3, Color.LightBlue) };
        
        private ZipTag[] tags;
        public ZipTag[] Tags
        {
            get => tags ?? (tags = new ZipTag[] { new ZipTag(Message.TagSample1, Color.Pink), new ZipTag(Message.TagSample2, Color.LightGreen), new ZipTag(Message.TagSample3, Color.LightBlue) });
            set => tags = value;
        }
    }
}
