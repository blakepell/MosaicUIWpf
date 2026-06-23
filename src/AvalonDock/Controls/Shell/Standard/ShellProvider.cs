/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace AvalonDock.Controls.Shell.Standard
{
    /// <summary>Specifies shell folder attributes.</summary>
    [Flags]
    internal enum SFGAO : uint
    {
        /// <summary>The C A N C O P Y value.</summary>
        CANCOPY = 0x1,

        /// <summary>The C A N M O V E value.</summary>
        CANMOVE = 0x2,

        /// <summary>The C A N L I N K value.</summary>
        CANLINK = 0x4,

        /// <summary>The S T O R A G E value.</summary>
        STORAGE = 0x00000008,

        /// <summary>The C A N R E N A M E value.</summary>
        CANRENAME = 0x00000010,

        /// <summary>The C A N D E L E T E value.</summary>
        CANDELETE = 0x00000020,

        /// <summary>The H A S P R O P S H E E T value.</summary>
        HASPROPSHEET = 0x00000040,

        // unused = 0x00000080,

        /// <summary>The D R O P T A R G E T value.</summary>
        DROPTARGET = 0x00000100,

        /// <summary>The C A P A B I L I T Y M A S K value.</summary>
        CAPABILITYMASK = 0x00000177,

        // unused = 0x00000200,
        // unused = 0x00000400,
        // unused = 0x00000800,
        // unused = 0x00001000,

        /// <summary>The E N C R Y P T E D value.</summary>
        ENCRYPTED = 0x00002000,

        /// <summary>The I S S L O W value.</summary>
        ISSLOW = 0x00004000,

        /// <summary>The G H O S T E D value.</summary>
        GHOSTED = 0x00008000,

        /// <summary>The L I N K value.</summary>
        LINK = 0x00010000,

        /// <summary>The S H A R E value.</summary>
        SHARE = 0x00020000,

        /// <summary>The R E A D O N L Y value.</summary>
        READONLY = 0x00040000,

        /// <summary>The H I D D E N value.</summary>
        HIDDEN = 0x00080000,

        /// <summary>The D I S P L A Y A T T R M A S K value.</summary>
        DISPLAYATTRMASK = 0x000FC000,

        /// <summary>The F I L E S Y S A N C E S T O R value.</summary>
        FILESYSANCESTOR = 0x10000000,

        /// <summary>The F O L D E R value.</summary>
        FOLDER = 0x20000000,

        /// <summary>The F I L E S Y S T E M value.</summary>
        FILESYSTEM = 0x40000000,

        /// <summary>The H A S S U B F O L D E R value.</summary>
        HASSUBFOLDER = 0x80000000,

        /// <summary>The C O N T E N T S M A S K value.</summary>
        CONTENTSMASK = 0x80000000,

        /// <summary>The V A L I D A T E value.</summary>
        VALIDATE = 0x01000000,

        /// <summary>The R E M O V A B L E value.</summary>
        REMOVABLE = 0x02000000,

        /// <summary>The C O M P R E S S E D value.</summary>
        COMPRESSED = 0x04000000,

        /// <summary>The B R O W S A B L E value.</summary>
        BROWSABLE = 0x08000000,

        /// <summary>The N O N E N U M E R A T E D value.</summary>
        NONENUMERATED = 0x00100000,

        /// <summary>The N E W C O N T E N T value.</summary>
        NEWCONTENT = 0x00200000,

        /// <summary>The C A N M O N I K E R value.</summary>
        CANMONIKER = 0x00400000,

        /// <summary>The H A S S T O R A G E value.</summary>
        HASSTORAGE = 0x00400000,

        /// <summary>The S T R E A M value.</summary>
        STREAM = 0x00400000,

        /// <summary>The S T O R A G E A N C E S T O R value.</summary>
        STORAGEANCESTOR = 0x00800000,

        /// <summary>The S T O R A G E C A P M A S K value.</summary>
        STORAGECAPMASK = 0x70C50008,

        /// <summary>The P K E Y S F G A O M A S K value.</summary>
        PKEYSFGAOMASK = 0x81044000,
    }

    /// <summary>Specifies shell item comparison hints.</summary>
    internal enum SICHINT : uint
    {
        /// <summary>The D I S P L A Y value.</summary>
        DISPLAY = 0x00000000,

        /// <summary>The A L L F I E L D S value.</summary>
        ALLFIELDS = 0x80000000,

        /// <summary>The C A N O N I C A L value.</summary>
        CANONICAL = 0x10000000,

        /// <summary>The TEST FILESYSPATH IF NOT EQUAL value.</summary>
        TEST_FILESYSPATH_IF_NOT_EQUAL = 0x20000000,
    }

    /// <summary>Specifies shell item display name formats.</summary>
    internal enum SIGDN : uint
    {
        // lower word (& with 0xFFFF)

        /// <summary>SHGDN_NORMAL.</summary>
        NORMALDISPLAY = 0x00000000, // SHGDN_NORMAL

        /// <summary>SHGDN_INFOLDER | SHGDN_FORPARSING.</summary>
        PARENTRELATIVEPARSING = 0x80018001, // SHGDN_INFOLDER | SHGDN_FORPARSING

        /// <summary>SHGDN_FORPARSING.</summary>
        DESKTOPABSOLUTEPARSING = 0x80028000, // SHGDN_FORPARSING

        /// <summary>SHGDN_INFOLDER | SHGDN_FOREDITING.</summary>
        PARENTRELATIVEEDITING = 0x80031001, // SHGDN_INFOLDER | SHGDN_FOREDITING

        /// <summary>SHGDN_FORPARSING | SHGDN_FORADDRESSBAR.</summary>
        DESKTOPABSOLUTEEDITING = 0x8004c000, // SHGDN_FORPARSING | SHGDN_FORADDRESSBAR

        /// <summary>SHGDN_FORPARSING.</summary>
        FILESYSPATH = 0x80058000, // SHGDN_FORPARSING

        /// <summary>SHGDN_FORPARSING.</summary>
        URL = 0x80068000, // SHGDN_FORPARSING

        /// <summary>SHGDN_INFOLDER | SHGDN_FORPARSING | SHGDN_FORADDRESSBAR.</summary>
        PARENTRELATIVEFORADDRESSBAR = 0x8007c001, // SHGDN_INFOLDER | SHGDN_FORPARSING | SHGDN_FORADDRESSBAR

        /// <summary>SHGDN_INFOLDER.</summary>
        PARENTRELATIVE = 0x80080001, // SHGDN_INFOLDER
    }

    /// <summary>Property key structure.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct PKEY
    {
        /// <summary>The format identifier.</summary>
        private readonly Guid _fmtid;

        /// <summary>The property identifier.</summary>
        private readonly uint _pid;

        /// <summary>Initializes a new instance of the <see cref="PKEY"/> struct.</summary>
        /// <param name="fmtid">The format identifier.</param>
        /// <param name="pid">The property identifier.</param>
        public PKEY(Guid fmtid, uint pid)
        {
            _fmtid = fmtid;
            _pid = pid;
        }

        /// <summary>Property key for the title.</summary>
        public static readonly PKEY Title = new(new Guid("F29F85E0-4FF9-1068-AB91-08002B27B3D9"), 2);

        /// <summary>Property key for the AppUserModelID.</summary>
        public static readonly PKEY AppUserModel_ID = new(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);

        /// <summary>Property key for destination list separators.</summary>
        public static readonly PKEY AppUserModel_IsDestListSeparator = new(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 6);

        /// <summary>Property key for the relaunch command.</summary>
        public static readonly PKEY AppUserModel_RelaunchCommand = new(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 2);

        /// <summary>Property key for the relaunch display name resource.</summary>
        public static readonly PKEY AppUserModel_RelaunchDisplayNameResource = new(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 4);

        /// <summary>Property key for the relaunch icon resource.</summary>
        public static readonly PKEY AppUserModel_RelaunchIconResource = new(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 3);
    }

    /// <summary>COM interface for enumerating item identifier lists.</summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid(IID.EnumIdList)]
    internal interface IEnumIDList
    {
        /// <summary>Retrieves the next set of items.</summary>
        /// <param name="celt">The number of elements.</param>
        /// <param name="rgelt">The retrieved elements.</param>
        /// <param name="pceltFetched">The number of fetched elements.</param>
        /// <returns>Returns the HRESULT from the operation.</returns>
        [PreserveSig]
        HRESULT Next(uint celt, out IntPtr rgelt, out int pceltFetched);

        /// <summary>Skips the specified number of items.</summary>
        /// <param name="celt">The number of elements.</param>
        /// <returns>Returns the skip result.</returns>
        [PreserveSig]
        HRESULT Skip(uint celt);

        /// <summary>Resets the enumeration sequence.</summary>
        void Reset();

        /// <summary>Creates a copy of the enumerator.</summary>
        /// <param name="ppenum">The cloned enumerator.</param>
        void Clone([Out, MarshalAs(UnmanagedType.Interface)] out IEnumIDList ppenum);
    }

    /// <summary>COM interface for enumerating shell objects.</summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid(IID.EnumObjects)]
    internal interface IEnumObjects
    {
        // [local]
        // This signature might not work... Hopefully don't need this interface though.

        /// <summary>Retrieves the next set of items.</summary>
        /// <param name="celt">The number of elements.</param>
        /// <param name="riid">The interface identifier.</param>
        /// <param name="rgelt">The retrieved elements.</param>
        /// <param name="pceltFetched">The number of fetched elements.</param>
        void Next(uint celt, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.IUnknown, IidParameterIndex = 1, SizeParamIndex = 0)] object[] rgelt, [Out] out uint pceltFetched);

        /// <summary>Skips the specified number of items.</summary>
        /// <param name="celt">The number of elements.</param>
        void Skip(uint celt);

        /// <summary>Resets the enumeration sequence.</summary>
        void Reset();

        /// <summary>Creates a copy of the enumerator.</summary>
        /// <returns>Returns the cloned enumerator.</returns>
        IEnumObjects Clone();
    }

    /// <summary>COM interface for shell item operations.</summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid(IID.ShellItem)]
    internal interface IShellItem
    {
        /// <summary>Binds to a handler for the shell item.</summary>
        /// <param name="pbc">The bind context.</param>
        /// <param name="bhid">The handler identifier.</param>
        /// <param name="riid">The interface identifier.</param>
        /// <returns>Returns the requested handler interface.</returns>
        [return: MarshalAs(UnmanagedType.Interface)]
        object BindToHandler(IBindCtx pbc, [In] ref Guid bhid, [In] ref Guid riid);

        /// <summary>Gets the parent shell item.</summary>
        /// <returns>Returns the parent shell item.</returns>
        IShellItem GetParent();

        /// <summary>Gets the display name.</summary>
        /// <param name="sigdnName">The display name format.</param>
        /// <returns>Returns the display name.</returns>
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetDisplayName(SIGDN sigdnName);

        /// <summary>Gets the specified attributes.</summary>
        /// <param name="sfgaoMask">The attribute mask.</param>
        /// <returns>Returns the requested attributes.</returns>
        SFGAO GetAttributes(SFGAO sfgaoMask);

        /// <summary>Compares this item with another shell item.</summary>
        /// <param name="psi">The shell item to compare.</param>
        /// <param name="hint">The comparison hint.</param>
        /// <returns>Returns the comparison result.</returns>
        int Compare(IShellItem psi, SICHINT hint);
    }

    /// <summary>COM interface for Windows shell links.</summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid(IID.ShellLink)]
    internal interface IShellLinkW
    {
        /// <summary>Gets the shell link path.</summary>
        /// <param name="pszFile">The path text or buffer.</param>
        /// <param name="cchMaxPath">The buffer length.</param>
        /// <param name="pfd">The file data buffer.</param>
        /// <param name="fFlags">The option flags.</param>
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, [In, Out] WIN32_FIND_DATAW pfd, SLGP fFlags);

        /// <summary>Gets the item identifier list.</summary>
        /// <param name="ppidl">The item identifier list.</param>
        void GetIDList(out IntPtr ppidl);

        /// <summary>Sets the item identifier list.</summary>
        /// <param name="pidl">The item identifier list.</param>
        void SetIDList(IntPtr pidl);

        /// <summary>Gets the shell link description.</summary>
        /// <param name="pszFile">The path text or buffer.</param>
        /// <param name="cchMaxName">The buffer length.</param>
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxName);

        /// <summary>Sets the shell link description.</summary>
        /// <param name="pszName">The name value.</param>
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        /// <summary>Gets the working directory.</summary>
        /// <param name="pszDir">The working directory.</param>
        /// <param name="cchMaxPath">The buffer length.</param>
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

        /// <summary>Sets the working directory.</summary>
        /// <param name="pszDir">The working directory.</param>
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        /// <summary>Gets the shell link arguments.</summary>
        /// <param name="pszArgs">The argument text or buffer.</param>
        /// <param name="cchMaxPath">The buffer length.</param>
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

        /// <summary>Sets the shell link arguments.</summary>
        /// <param name="pszArgs">The argument text or buffer.</param>
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        /// <summary>Gets the hot key.</summary>
        /// <returns>Returns the hot key.</returns>
        short GetHotKey();

        /// <summary>Sets the hot key.</summary>
        /// <param name="wHotKey">The hot key value.</param>
        void SetHotKey(short wHotKey);

        /// <summary>Gets the show command.</summary>
        /// <returns>Returns the show command.</returns>
        uint GetShowCmd();

        /// <summary>Sets the show command.</summary>
        /// <param name="iShowCmd">The show command.</param>
        void SetShowCmd(uint iShowCmd);

        /// <summary>Gets the icon location.</summary>
        /// <param name="pszIconPath">The icon path or buffer.</param>
        /// <param name="cchIconPath">The buffer length.</param>
        /// <param name="piIcon">The icon index.</param>
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

        /// <summary>Sets the icon location.</summary>
        /// <param name="pszIconPath">The icon path or buffer.</param>
        /// <param name="iIcon">The icon index.</param>
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        /// <summary>Sets the relative path.</summary>
        /// <param name="pszPathRel">The relative path.</param>
        /// <param name="dwReserved">The reserved value.</param>
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

        /// <summary>Resolves the shell link.</summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="fFlags">The option flags.</param>
        void Resolve(IntPtr hwnd, uint fFlags);

        /// <summary>Sets the shell link path.</summary>
        /// <param name="pszFile">The path text or buffer.</param>
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}