
Imports System.Collections.Concurrent
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic

'Maps the Windows tool and plugin file names of the package definitions to Linux names
'and searches the standard Linux locations of executables, libraries and plugins.
Public Class LinuxNames
    Shared ReadOnly ExeNames As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
        {"NVEncC64.exe", "nvencc"},
        {"QSVEncC64.exe", "qsvencc"},
        {"VCEEncC64.exe", "vceencc"},
        {"mpvnet.exe", "mpv"},
        {"MediaInfoNET.exe", "mediainfo-gui"},
        {"python.exe", "python3"},
        {"7za.exe", "7z"},
        {"AVSMeter64.exe", "avsmeter"},
        {"avs2pipemod64.exe", "avs2pipemod"},
        {"mpc-be64.exe", "mpc-be"},
        {"qaac64.exe", "qaac"},
        {"SubtitleEdit.exe", "subtitleedit"},
        {"neroAacEnc.exe", "neroAacEnc"}
    }

    'distributions package some tools under different names
    Shared ReadOnly ExeAlternatives As New Dictionary(Of String, String())(StringComparer.OrdinalIgnoreCase) From {
        {"7z", {"7zz", "7za"}}
    }

    Shared Function GetExeAlternatives(name As String) As String()
        Return If(ExeAlternatives.ContainsKey(name), ExeAlternatives(name), {})
    End Function

    Shared ReadOnly LibraryNames As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
        {"MediaInfo.dll", "libmediainfo.so.0"},
        {"libvapoursynth.dll", "libvapoursynth.so"},
        {"AviSynth.dll", "libavisynth.so"},
        {"libfftw3-3.dll", "libfftw3f.so.3"},
        {"CoreAudioToolbox.dll", ""},
        {"msvcp100.dll", ""},
        {"msvcp110.dll", ""},
        {"msvcp120.dll", ""},
        {"msvcp140.dll", ""}
    }

    'Linux file names of plugins that don't follow the lib<name>.so convention
    Shared ReadOnly PluginNames As New Dictionary(Of String, String())(StringComparer.OrdinalIgnoreCase) From {
        {"ffms2.dll", {"libffms2.so"}},
        {"LSMASHSource.dll", {"libvslsmashsource.so", "libLSMASHSource.so", "liblsmashsource.so"}},
        {"BestSource.dll", {"libbestsource.so"}},
        {"flash3kyuu_deband.dll", {"libf3kdb.so", "libflash3kyuu_deband.so"}},
        {"neo-f3kdb.dll", {"libneo-f3kdb.so", "libneo_f3kdb.so"}},
        {"neo-fft3d.dll", {"libneo-fft3d.so", "libneo_fft3d.so"}},
        {"neo-dfttest.dll", {"libneo-dfttest.so", "libneo_dfttest.so"}},
        {"RemoveGrainVS.dll", {"librgvs.so", "libremovegrain.so"}},
        {"libvs_placebo.dll", {"libvs_placebo.so", "libvsplacebo.so"}},
        {"avs_libplacebo.dll", {"libavs_libplacebo.so"}},
        {"Shader-x64.dll", {}},
        {"FFT3dGPU.dll", {}},
        {"VSFilter.dll", {}},
        {"VSFilterMod.dll", {}}
    }

    Shared Function GetFilename(windowsName As String, isPlugin As Boolean) As String
        If String.IsNullOrEmpty(windowsName) Then
            Return windowsName
        End If

        Dim ext = Path.GetExtension(windowsName).ToLowerInvariant
        Dim base = Path.GetFileNameWithoutExtension(windowsName)

        Select Case ext
            Case ".exe"
                If ExeNames.ContainsKey(windowsName) Then
                    Return ExeNames(windowsName)
                End If

                Return base
            Case ".dll"
                If isPlugin Then
                    If PluginNames.ContainsKey(windowsName) Then
                        Dim names = PluginNames(windowsName)
                        Return If(names.Length > 0, names(0), "")
                    End If

                    Return If(base.StartsWith("lib"), base, "lib" + base) + ".so"
                End If

                If LibraryNames.ContainsKey(windowsName) Then
                    Return LibraryNames(windowsName)
                End If

                Return If(base.StartsWith("lib"), base, "lib" + base).ToLowerInvariant + ".so"
            Case ".ax"
                Return ""
        End Select

        Return windowsName
    End Function

    Shared Function GetPluginCandidates(pack As Package) As List(Of String)
        Dim ret As New List(Of String)
        Dim windowsName = pack.WindowsFilename

        If String.IsNullOrEmpty(windowsName) Then
            Return ret
        End If

        If Not windowsName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) Then
            ret.Add(windowsName)
            Return ret
        End If

        If PluginNames.ContainsKey(windowsName) Then
            ret.AddRange(PluginNames(windowsName))
            Return ret
        End If

        Dim base = Path.GetFileNameWithoutExtension(windowsName)
        Dim libBase = If(base.StartsWith("lib", StringComparison.OrdinalIgnoreCase), base, "lib" + base)

        For Each i In {libBase + ".so", libBase.ToLowerInvariant + ".so", base + ".so", base.ToLowerInvariant + ".so",
            libBase.Replace("-", "_") + ".so", libBase.ToLowerInvariant.Replace("-", "") + ".so"}

            If Not ret.Contains(i) Then
                ret.Add(i)
            End If
        Next

        Return ret
    End Function

    Private Shared LibraryDirsValue As String()

    'standard and multiarch directories, the directories of the dynamic linker configuration,
    'which every glibc distribution maintains, and the profiles of Nix and Guix
    Shared ReadOnly Property LibraryDirs As String()
        Get
            If LibraryDirsValue Is Nothing Then
                Dim ret As New List(Of String)
                Dim ldPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")

                If ldPath <> "" Then
                    ret.AddRange(ldPath.Split({":"c}, StringSplitOptions.RemoveEmptyEntries))
                End If

                Dim triplet = MultiarchTriplet
                ret.AddRange({"/usr/local/lib", "/usr/local/lib64", "/usr/local/lib/" + triplet, "/usr/lib", "/usr/lib64",
                    "/usr/lib/" + triplet, "/lib/" + triplet, "/lib64", "/lib"})
                ret.AddRange(GetLdSoConfDirs("/etc/ld.so.conf", New HashSet(Of String)))
                ret.AddRange({Path.Combine(Folder.Home, ".nix-profile", "lib"), "/run/current-system/sw/lib",
                    "/etc/profiles/per-user/" + Environment.UserName + "/lib", Path.Combine(Folder.Home, ".guix-profile", "lib")})

                '32-bit libraries can't be loaded by the 64-bit process
                LibraryDirsValue = ret.Where(Function(x) x.StartsWith("/") AndAlso Not x.Contains("lib32") AndAlso
                    Not x.Contains("i386-linux-gnu") AndAlso Directory.Exists(x)).Distinct.ToArray
            End If

            Return LibraryDirsValue
        End Get
    End Property

    'Debian based distributions put libraries into directories named after the architecture
    Shared ReadOnly Property MultiarchTriplet As String
        Get
            Select Case RuntimeInformation.ProcessArchitecture
                Case Architecture.Arm64
                    Return "aarch64-linux-gnu"
                Case Architecture.Arm
                    Return "arm-linux-gnueabihf"
                Case Architecture.RiscV64
                    Return "riscv64-linux-gnu"
                Case Architecture.LoongArch64
                    Return "loongarch64-linux-gnu"
                Case Architecture.Ppc64le
                    Return "powerpc64le-linux-gnu"
                Case Architecture.S390x
                    Return "s390x-linux-gnu"
                Case Else
                    Return "x86_64-linux-gnu"
            End Select
        End Get
    End Property

    Shared Function GetLdSoConfDirs(confFile As String, visited As HashSet(Of String)) As List(Of String)
        Dim ret As New List(Of String)

        If Not visited.Add(confFile) OrElse Not File.Exists(confFile) Then
            Return ret
        End If

        Try
            For Each line In File.ReadAllLines(confFile)
                Dim value = line.Split("#"c)(0).Trim

                If value.StartsWith("include ") Then
                    Dim pattern = value.Substring(8).Trim
                    Dim dir = Path.GetDirectoryName(pattern)

                    If dir <> "" AndAlso Directory.Exists(dir) Then
                        For Each i In Directory.GetFiles(dir, Path.GetFileName(pattern)).OrderBy(Function(x) x)
                            ret.AddRange(GetLdSoConfDirs(i, visited))
                        Next
                    End If
                ElseIf value <> "" Then
                    ret.Add(value.Split("="c)(0).Trim)
                End If
            Next
        Catch
        End Try

        Return ret
    End Function

    Shared ReadOnly Property VapourSynthPluginDirs As String()
        Get
            Dim ret As New List(Of String) From {
                Path.Combine(Folder.Settings, "Plugins", "VapourSynth"),
                Path.Combine(Folder.Settings, "Plugins", "Dual"),
                Path.Combine(Folder.Home, ".local", "lib", "vapoursynth"),
                Path.Combine(Folder.XdgDataHome, "vapoursynth", "plugins")
            }

            Dim extraPath = Environment.GetEnvironmentVariable("VAPOURSYNTH_EXTRA_PLUGIN_PATH")

            If extraPath <> "" Then
                ret.AddRange(extraPath.Split({":"c}, StringSplitOptions.RemoveEmptyEntries))
            End If

            For Each dir As String In LibraryDirs
                ret.Add(Path.Combine(dir, "vapoursynth"))
            Next

            'newer VapourSynth versions autoload plugins from the plugins folder of the Python module
            ret.AddRange(VapourSynthInfo.GetAutoloadedPlugins().Select(Function(x) Path.GetDirectoryName(x)))
            ret.AddRange(VapourSynthInfo.GetPythonPaths().Select(Function(x) Path.Combine(x, "vapoursynth", "plugins")))

            Return ret.Where(Function(x) x <> "" AndAlso Directory.Exists(x)).Distinct.ToArray
        End Get
    End Property

    Shared ReadOnly Property AviSynthPluginDirs As String()
        Get
            Dim ret As New List(Of String) From {
                Path.Combine(Folder.Settings, "Plugins", "AviSynth"),
                Path.Combine(Folder.Settings, "Plugins", "Dual"),
                Path.Combine(Folder.Home, ".local", "lib", "avisynth"),
                Path.Combine(Folder.Home, ".avisynth")
            }

            For Each dir As String In LibraryDirs
                ret.Add(Path.Combine(dir, "avisynth"))
            Next

            Return ret.Where(Function(x) Directory.Exists(x)).Distinct.ToArray
        End Get
    End Property

    Shared Function FindLibrary(name As String) As String
        If name = "" Then
            Return ""
        End If

        Dim dirs = LibraryDirs

        'VapourSynth installed with pip has its libraries in the folder of the Python module
        If name.StartsWith("libvapoursynth") Then
            dirs = dirs.Concat({VapourSynthInfo.GetModuleDir()}).Where(Function(x) x <> "").ToArray
        End If

        For Each dir As String In dirs
            Dim fp = Path.Combine(dir, name)

            If File.Exists(fp) Then
                Return fp
            End If
        Next

        'versioned libraries like libavisynth.so.11
        If name.EndsWith(".so") Then
            For Each dir As String In dirs
                Dim match = FindVersionedLibrary(dir, name)

                If match <> "" Then
                    Return match
                End If
            Next
        End If

        Return ""
    End Function

    Private Shared PythonModuleCache As New ConcurrentDictionary(Of String, String)

    Shared Function FindPlugin(pack As Package) As String
        Dim plugin = TryCast(pack, PluginPackage)

        If plugin Is Nothing Then
            Return ""
        End If

        Dim ext = pack.WindowsFilename?.Ext

        'Python scripts are found by Python when they are installed in site-packages
        If ext = "py" Then
            For Each dir As String In VapourSynthPluginDirs.Select(Function(x) Path.Combine(x, "Scripts")).Concat(VapourSynthPluginDirs)
                Dim fp = Path.Combine(dir, pack.WindowsFilename)

                If File.Exists(fp) Then
                    Return fp
                End If
            Next

            Return FindPythonModule(Path.GetFileNameWithoutExtension(pack.WindowsFilename))
        End If

        If ext = "avsi" OrElse ext = "avs" Then
            For Each dir As String In AviSynthPluginDirs
                Dim fp = Path.Combine(dir, pack.WindowsFilename)

                If File.Exists(fp) Then
                    Return fp
                End If
            Next

            Return ""
        End If

        Dim dirs As New List(Of String)

        If Not plugin.VsFilterNames.NothingOrEmpty Then
            dirs.AddRange(VapourSynthPluginDirs)
        End If

        If Not plugin.AvsFilterNames.NothingOrEmpty Then
            dirs.AddRange(AviSynthPluginDirs)
        End If

        dirs.AddRange(LibraryDirs)

        For Each dir As String In dirs.Distinct
            For Each name In GetPluginCandidates(pack)
                Dim fp = Path.Combine(dir, name)

                If File.Exists(fp) Then
                    Return fp
                End If
            Next
        Next

        'libraries like libffms2.so.5, the unversioned name only comes with the development package
        For Each dir As String In dirs.Distinct
            For Each name In GetPluginCandidates(pack).Where(Function(x) x.EndsWith(".so"))
                Dim match = FindVersionedLibrary(dir, name)

                If match <> "" Then
                    Return match
                End If
            Next
        Next

        Return ""
    End Function

    Private Shared ReadOnly VersionedLibraries As New ConcurrentDictionary(Of String, Dictionary(Of String, String))

    'finds libfoo.so.1 and libfoo.1.2.so, the name of AviSynth+ plugins built with CMake,
    'by libfoo.so, the folders are only read once
    Shared Function FindVersionedLibrary(dir As String, name As String) As String
        Dim libs = VersionedLibraries.GetOrAdd(dir, Function(key)
                                                        Dim ret As New Dictionary(Of String, String)

                                                        Try
                                                            For Each fp In Directory.EnumerateFiles(key, "*.so*")
                                                                Dim fileName = Path.GetFileName(fp)
                                                                Dim baseName As String

                                                                If fileName.Contains(".so.") Then
                                                                    baseName = fileName.Substring(0, fileName.IndexOf(".so.") + 3)
                                                                Else
                                                                    Dim match = Regex.Match(fileName, "^(.+?)\.v?\d+(\.\d+)*\.so$")
                                                                    If Not match.Success Then Continue For
                                                                    baseName = match.Groups(1).Value + ".so"
                                                                End If

                                                                Dim existing As String = Nothing

                                                                If Not ret.TryGetValue(baseName, existing) OrElse fp.Length < existing.Length Then
                                                                    ret(baseName) = fp
                                                                End If
                                                            Next
                                                        Catch
                                                        End Try

                                                        Return ret
                                                    End Function)

        Dim result As String = Nothing
        Return If(libs.TryGetValue(name, result), result, "")
    End Function

    'returns the path of a Python module of the Python used by VapourSynth
    Shared Function FindPythonModule(name As String) As String
        Return PythonModuleCache.GetOrAdd(name, Function(key)
                                                    For Each dir As String In VapourSynthInfo.GetPythonPaths()
                                                        For Each fp In {Path.Combine(dir, key + ".py"), Path.Combine(dir, key, "__init__.py")}
                                                            If File.Exists(fp) Then
                                                                Return fp
                                                            End If
                                                        Next
                                                    Next

                                                    Return ""
                                                End Function)
    End Function

    Shared Function GetInstallHint(pack As Package) As String
        If TypeOf pack Is PluginPackage Then
            Dim plugin = DirectCast(pack, PluginPackage)

            If Not plugin.VsFilterNames.NothingOrEmpty Then
                Return "install the VapourSynth plugin with your package manager or copy it to ~/.local/lib/vapoursynth"
            End If

            Return "install the AviSynth+ plugin with your package manager or copy it to ~/.local/lib/avisynth"
        End If

        If pack.Filename.Contains(".so") Then
            Return "install the library with your package manager"
        End If

        Return "install it with your package manager or define a custom path"
    End Function
End Class

'Information about the VapourSynth installation, queried with vspipe
'because vspipe uses the Python version VapourSynth was built for.
Public Class VapourSynthInfo
    Private Shared Namespaces As HashSet(Of String)
    Private Shared PythonPaths As String()
    Private Shared PluginPaths As String()
    Private Shared ModuleDir As String
    Private Shared ReadOnly LockObject As New Object

    Shared Sub Reset()
        SyncLock LockObject
            Namespaces = Nothing
            PythonPaths = Nothing
            PluginPaths = Nothing
            ModuleDir = Nothing
        End SyncLock
    End Sub

    Shared Function GetPluginNamespaces() As HashSet(Of String)
        SyncLock LockObject
            If Namespaces Is Nothing Then
                Query()
            End If

            Return Namespaces
        End SyncLock
    End Function

    'paths of the plugins VapourSynth loads automatically
    Shared Function GetAutoloadedPlugins() As String()
        SyncLock LockObject
            If PluginPaths Is Nothing Then
                Query()
            End If

            Return PluginPaths
        End SyncLock
    End Function

    'folder of the VapourSynth Python module, with pip installations it contains the libraries
    Shared Function GetModuleDir() As String
        SyncLock LockObject
            If ModuleDir Is Nothing Then
                Query()
            End If

            Return ModuleDir
        End SyncLock
    End Function

    Shared Function GetPythonPaths() As String()
        SyncLock LockObject
            If PythonPaths Is Nothing Then
                Query()
            End If

            Return PythonPaths
        End SyncLock
    End Function

    Private Shared Sub Query()
        Namespaces = New HashSet(Of String)
        PythonPaths = {}
        PluginPaths = {}
        ModuleDir = ""

        Dim vspipe = Package.vspipe.Path

        If vspipe = "" Then
            Exit Sub
        End If

        Dim dir = Path.Combine(Folder.Temp, "staxrip-" + Environment.UserName)
        Directory.CreateDirectory(dir)
        Dim scriptPath = Path.Combine(dir, "vs-info.vpy")
        Dim outputPath = Path.Combine(dir, "vs-info.txt")

        Dim code = "import sys" + vbLf +
            "import vapoursynth as vs" + vbLf +
            "core = vs.core" + vbLf +
            "with open(r'" + outputPath + "', 'w') as f:" + vbLf +
            "    f.write('\n'.join('ns:' + p.namespace for p in core.plugins()) + '\n')" + vbLf +
            "    f.write('\n'.join('path:' + p for p in sys.path if p) + '\n')" + vbLf +
            "    f.write('\n'.join('plugin:' + p.plugin_path for p in core.plugins() if getattr(p, 'plugin_path', None)) + '\n')" + vbLf +
            "    f.write('moddir:' + __import__('os').path.dirname(vs.__file__) + '\n')" + vbLf +
            "core.std.BlankClip(length=1).set_output()" + vbLf

        Try
            FileHelp.Delete(outputPath)
            code.WriteFileUTF8(scriptPath)
            ProcessHelp.GetConsoleOutput(vspipe, "--info " + scriptPath.Escape + " -")

            If File.Exists(outputPath) Then
                Dim paths As New List(Of String)
                Dim plugins As New List(Of String)

                For Each line In File.ReadAllLines(outputPath)
                    If line.StartsWith("ns:") Then
                        Namespaces.Add(line.Substring(3))
                    ElseIf line.StartsWith("path:") Then
                        paths.Add(line.Substring(5))
                    ElseIf line.StartsWith("plugin:") Then
                        plugins.Add(line.Substring(7))
                    ElseIf line.StartsWith("moddir:") Then
                        ModuleDir = line.Substring(7)
                    End If
                Next

                PythonPaths = paths.ToArray
                PluginPaths = plugins.ToArray
            End If
        Catch
        End Try
    End Sub
End Class
