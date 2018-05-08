using Core.Enums;
using System;
using System.Windows.Forms;


/*############################################################################*
 *                            Execute on Start                                *
 *############################################################################*/


namespace NeuralNet.Project
{
    static class MGMT
    {
        [STAThread]
        static void Main()
        {
            //SET MAIN FONT 
            Fonts.setMainFont(Fonts.getFontAgencyFB(16));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new NetMain());
        }
    }
}
