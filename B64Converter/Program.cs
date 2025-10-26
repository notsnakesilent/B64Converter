using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace B64Converter
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1 || !File.Exists(args[0]))
            {
                Console.WriteLine("Usage: b64converter <path-to-assembly>");
                return 1;
            }

            var inputPath = Path.GetFullPath(args[0]);
            ModuleDefMD mod;
            try
            {
                mod = ModuleDefMD.Load(File.ReadAllBytes(inputPath));
            }
            catch (Exception e)
            {
                Console.WriteLine("Load error: " + e.Message);
                return 1;
            }

            int patches = 0;

            foreach (var type in mod.GetTypes())
            {
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    var instrs = method.Body.Instructions;

                    for (int i = 0; i <= instrs.Count - 4; i++)
                    {
                        Func<int, string> ldstrAt = idx =>
                        {
                            return instrs[idx].OpCode == OpCodes.Ldstr ? instrs[idx].Operand as string : null;
                        };

                        Func<int, IMethod> callAt = idx =>
                        {
                            if (instrs[idx].OpCode == OpCodes.Call || instrs[idx].OpCode == OpCodes.Callvirt)
                                return instrs[idx].Operand as IMethod;
                            return null;
                        };

                        Func<IMethod, bool> isFromB64 = m =>
                            m != null && m.Name == "FromBase64String" &&
                            m.DeclaringType != null && m.DeclaringType.FullName == "System.Convert";

                        Func<IMethod, bool> isEncGetter = m =>
                            m != null &&
                            !object.ReferenceEquals(m.Name, null) &&
                            m.Name.ToString().StartsWith("get_") &&
                            m.DeclaringType != null && m.DeclaringType.FullName == "System.Text.Encoding";

                        Func<IMethod, bool> isEncGetString = m =>
                            m != null && m.Name == "GetString" &&
                            m.DeclaringType != null && m.DeclaringType.FullName == "System.Text.Encoding";

                        bool matchA =
                            isEncGetter(callAt(i + 0)) &&
                            ldstrAt(i + 1) != null &&
                            isFromB64(callAt(i + 2)) &&
                            isEncGetString(callAt(i + 3));

                        bool matchB =
                            ldstrAt(i + 0) != null &&
                            isFromB64(callAt(i + 1)) &&
                            isEncGetter(callAt(i + 2)) &&
                            isEncGetString(callAt(i + 3));

                        if (!matchA && !matchB) continue;

                        int ldstrIndex = matchA ? (i + 1) : (i + 0);
                        var b64 = ldstrAt(ldstrIndex);
                        if (b64 == null) continue;

                        byte[] bytes;
                        try { bytes = Convert.FromBase64String(b64); }
                        catch { continue; }

                        string decoded;
                        try { decoded = Encoding.UTF8.GetString(bytes); }
                        catch { decoded = Encoding.ASCII.GetString(bytes); }

                        if (!LooksPrintable(decoded)) continue;

                        instrs[i + 0].OpCode = OpCodes.Ldstr;
                        instrs[i + 0].Operand = decoded;
                        for (int k = 1; k < 4; k++)
                        {
                            instrs[i + k].OpCode = OpCodes.Nop;
                            instrs[i + k].Operand = null;
                        }

                        patches++;
                        i += 3;
                    }

                    method.Body.SimplifyBranches();
                    method.Body.OptimizeBranches();
                }
            }

            if (patches == 0) Console.WriteLine("No replacements found.");
            else Console.WriteLine("Replacements: " + patches);

            var dir = Path.GetDirectoryName(inputPath);
            var name = Path.GetFileNameWithoutExtension(inputPath);
            var ext = Path.GetExtension(inputPath);
            var outPath = Path.Combine(dir, name + "_converted" + ext);

            try
            {
                mod.Write(outPath);
                Console.WriteLine("Saved: " + outPath);
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Write error: " + e.Message);
                return 2;
            }
        }

        static bool LooksPrintable(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            int printable = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch == '\t' || ch == '\n' || ch == '\r') { printable++; continue; }
                if (ch >= 32 && ch < 127) { printable++; continue; }
                if ("ñÑáéíóúÁÉÍÓÚ°".IndexOf(ch) >= 0) { printable++; continue; }
            }
            return printable >= (int)(s.Length * 0.9);
        }

    }
}
