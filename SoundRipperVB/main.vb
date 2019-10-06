﻿Module main

    'globals
    Dim addlinetoprojectlog As String
    'Dim newprojectlog As String
    Dim databankcount As Integer

    Function FindDataStart(ByVal sndfileloc As String, ByVal dataWrap As Byte())
        Dim w() As Byte = IO.File.ReadAllBytes(sndfileloc) 'read file as array of bytes
        Dim wFound As Boolean = True
        Dim fsloc As New IO.FileStream(sndfileloc, IO.FileMode.Open)
        Dim binary_readerloc As New IO.BinaryReader(fsloc)
        Dim wrapByte As Byte()
        Dim wrapSize, bankcount As Integer
        Dim strDataWrap As String = dataWrap.ToString()
        Dim wavdatastart As Integer() = New Integer() {}


        bankcount = -1

        For i As Integer = 0 To w.Length - dataWrap.Length - 1
            If w(i) = dataWrap(0) Then
                wFound = True
                For j As Integer = 0 To dataWrap.Length - 1
                    If w(i + j) <> dataWrap(j) Then
                        wFound = False
                        Exit For
                    End If
                Next
                If wFound Then
                    bankcount += 1

                    'Read datasize, convert to uint32
                    fsloc.Position = i + 4
                    wrapByte = binary_readerloc.ReadBytes(4)
                    wrapSize = BitConverter.ToUInt32(wrapByte, 0)

                    ReDim Preserve wavdatastart(bankcount)
                    wavdatastart(bankcount) = i + 8

                    addlinetoprojectlog = strDataWrap & " data found at byte: " & i & " Size: " & wrapSize & vbCr
                    addlinetoprojectlog = "Wrap data found at byte: " & i + 8 & " Size: " & wrapSize & vbCr
                    'newprojectlog = newprojectlog & vbCr & addlinetoprojectlog
                    UpdateProjectLog(addlinetoprojectlog)



                    'I hope there's only one bank.  Oh wait, there isn't.  Darn it

                End If
            End If
        Next



        fsloc.Close()
        binary_readerloc.Close()

        databankcount = bankcount
        Return wavdatastart 'returns exact array byte of wav data 
    End Function

    Function AddHeader(ByVal HeaderType As String, ByVal Size As UInt32, ByVal SampleRate As UInt32)
        If HeaderType = "wavPCM16" Then 'standard bank

            'File header structure is as follows:
            'RIFF, size of file - 8 bytes (uint32)
            'WAVEfmt + 0x20
            'chunk size (generally 16 (0x10) for regular PCM, 20(0x14 for IMA ADPCM) --(uint32)
            'format (1 (0x01) for PCM16, 17 (0x11) for IMA ADPCM) (ushort)
            'channels (1 for mono (default here), 2 for stereo) (ushort)
            'Sample Rate (44100hz or 22050hz are most common) (uint32)
            'Bits/sample (sample rate * block align, but IMA ADPCM generally is samplerate / 2 because i don't understand (uint32)
            'block align (2048 (0x0008) for streams, otherwise just 2 (0x02) (ushort)
            'bits/sample (varies, default 4 for IMA ADPCM, 16 for PCM16, however I can only get some streams to work when I use 1...
            'there's some bytes here in sample files called format chunk that I have no clue what they are

            'optional chunks (IMA ADPCM)
            'fact
            '4byte (or more if you have more info) (0x04) (uint32)
            'uncompressed size (uint32)



            'Generate wav header
            Dim bufsize As UInt32 = Size + 28
            Dim SampleRatex2 As UInt32 = SampleRate * 2
            Dim RIFFsize As Byte() = BitConverter.GetBytes(bufsize)
            Dim ByteSize As Byte() = BitConverter.GetBytes(Size)
            Dim ByteSampleRate As Byte() = BitConverter.GetBytes(SampleRate)
            Dim ByteSampleRatex2 As Byte() = BitConverter.GetBytes(SampleRatex2)

            'VB doesn't like byte() to byte, so we're just going to eat up more memory here and make more variables
            Dim wavHeader As Byte() = {82, 73, 70, 70} ', RIFFsize, 87, 65, 86, 69, 102, 109, 116, 16, 0, 0, 0, 1, 0, 1, 0, ByteSampleRate, ByteSampleRatex2, 2, 0, 1, 0, 100, 97, 116, 97, ByteSize}
            Dim wavHeader2 As Byte() = {87, 65, 86, 69, 102, 109, 116, 32, 16, 0, 0, 0, 1, 0, 1, 0}
            Dim wavHeader3 As Byte() = {2, 0, 16, 0, 100, 97, 116, 97}

            'rawr concatentation - RIP readability
            Dim totalHeader As Byte() = wavHeader.Concat(RIFFsize).Concat(wavHeader2).Concat(ByteSampleRate).Concat(ByteSampleRatex2).Concat(wavHeader3).Concat(ByteSize).ToArray()


            'logging, for the love of pete please disable this part before production. we're doing enough logging for testing already
            'addlinetoprojectlog = totalHeader.ToString()
            'UpdateProjectLog(addlinetoprojectlog)

            Return totalHeader 'return byte array
        ElseIf HeaderType = "wavIPCM" Then 'ima adpcm for stream files
            'this is super ghetto and maybe should find a separate library
            'Generate wav header
            Dim bufsize As UInt32 = Size + 40
            Dim SampleRatediv2 As UInt32 = SampleRate / 2
            Dim resampleSize As UInt32 = SampleRate * 4 'Currently janking this at *4, but technically it's in the stm header
            Dim RIFFsize As Byte() = BitConverter.GetBytes(bufsize)
            Dim ByteSize As Byte() = BitConverter.GetBytes(Size)
            Dim ByteSampleRate As Byte() = BitConverter.GetBytes(SampleRate)
            Dim ByteSampleRatediv2 As Byte() = BitConverter.GetBytes(SampleRatediv2)

            Dim factSize As Byte() = BitConverter.GetBytes(resampleSize)

            'VB doesn't like byte() to byte, so we're just going to eat up more memory here and make more variables
            Dim wavHeader As Byte() = {82, 73, 70, 70} ', RIFFsize, 87, 65, 86, 69, 102, 109, 116, 16, 0, 0, 0, 1, 0, 1, 0, ByteSampleRate, ByteSampleRatex2, 2, 0, 1, 0, 100, 97, 116, 97, ByteSize}
            Dim wavHeader2 As Byte() = {87, 65, 86, 69, 102, 109, 116, 32, 20, 0, 0, 0, 17, 0, 1, 0}
            Dim wavHeader3 As Byte() = {36, 0, 4, 0, 2, 0, 64, 0}
            Dim wavHeader4 As Byte() = {100, 97, 116, 97}

            'rawr concatentation - RIP readability
            Dim totalHeader As Byte() = wavHeader.Concat(RIFFsize).Concat(wavHeader2).Concat(ByteSampleRate).Concat(ByteSampleRatediv2).Concat(wavHeader3).Concat(wavHeader4).Concat(ByteSize).ToArray()

            Return totalHeader 'return byte array
        ElseIf HeaderType = "bik" Then
            'RIP this. biks stored byte for byte.

            'bik header info, copied from wiki.multimedia.cx
            'bytes 0-2     file signature ('BIK', or 'KB2' for Bink Video 2)
            'Byte 3        Bink Video codec revision (0x69, version i for BF1)
            ' bytes 4 - 7     file size Not including the first 8 bytes
            'bytes 8 - 11    number of frames (600 in ATAT_screen, 712 bes1fly)
            'bytes 12 - 15   largest frame size in bytes (63804, 62204)
            'bytes 16 - 19   number of frames again? (same as above)
            'bytes 20 - 23   video width (less than Or equal to 32767)  (800, like in 800x600)
            'bytes 24 - 27   video height (less than Or equal to 32767) (600, like in 800x600)
            'bytes 28 - 31   video frames per second dividend (2997 for both)
            'bytes 32 - 35   video frames per second divider (100 for both)
            'bytes 36 - 39   video flags (all 0)
            '  bits 28 - 31: width And height scaling  (last byte)
            '1 = 2x height doubled
            '2 = 2x height interlaced
            '3 = 2x width doubled
            '4 = 2x width And height-doubled
            '5 = 2x width And height-interlaced
            '        bit 20: has alpha plane
            '  bit 17: grayscale
            '  bytes 40 - 43   number of audio tracks (less than Or equal to 256) (01 for audio

            'Generate bik header
            Return 0
        Else
            Return 0
            Exit Function
        End If
    End Function

    Sub UpdateProjectLog(ByVal projlog As String)
        Dim file As System.IO.StreamWriter
        file = My.Computer.FileSystem.OpenTextFileWriter("test.txt", True)
        file.Write(projlog)
        file.Close()
    End Sub
    Public Sub Main()

#Region "CommandLineParameters"
        'Get the values of the command line in an array
        ' Index  Discription
        ' 0      Full path of executing prograsm with program name
        ' 1      First switch in command in your example -t
        ' 2      First value in command in your example text1
        ' 3      Second switch in command in your example -s
        ' 4      Second value in command in your example text2
        'Dim clArgs() As String = Environment.GetCommandLineArgs()
        ' Hold the command line values
        ' Dim type As String = String.Empty
        ' Test to see if two switchs and two values were passed in
        ' if yes parse the array
        'If clArgs.Count() = 2 Then
        'For i As Integer = 1 To 1 'only check 1 for now
        'If clArgs(i) = "-t" Then
        'Type = clArgs(i + 1)
        ' Else
        '            type = "wav" 'default as wav for now
        '  End If
        '  Next
        ' End If

        ' Console.WriteLine(type)
        ' Console.ReadLine()
#End Region


#Region "Body"

#Region "variables"
        System.IO.File.WriteAllText("test.txt", "")
        Dim sndfile As String = "gcw_tac_vo.str"
        Dim platform As String = "pc" 'only for videos as of now
        Dim wavtype As String
        Dim filetype As String = sndfile.Substring(sndfile.Length - 4)

        'This code is pretty arbitrary with what it picks for the dissection type
        If filetype = ".lvl" Or filetype = ".str" Then
            wavtype = "wavIPCM"
        ElseIf filetype = ".mvs" Then
            If platform = "ps2" Then
                wavtype = "pss"
                'NOTE: FOR PSS FILES - there is a goofy amount of bytes in between which I had to remove by hand
            Else
                wavtype = "bik"
            End If
        Else
            wavtype = "wavPCM16"
        End If

        addlinetoprojectlog = "Parsing " & sndfile & vbCr
        'newprojectlog = newprojectlog & vbCr & addlinetoprojectlog
        UpdateProjectLog(addlinetoprojectlog)

        Dim SearchStart As Byte()
        Dim dataWrapper As Byte()
        'Dim SearchStringStart As String

        'give us a starting search point
        If wavtype = "bik" Then
            SearchStart = {66, 73, 75, 105}
            'SearchStringStart = BitConverter.ToString(SearchStart)
            dataWrapper = {}
        ElseIf wavtype = "pss" Then
            SearchStart = {21, 54, 208, 131}
            'SearchStringStart = BitConverter.ToString(SearchStart)
            'SearchStringStart = "PSS"
            dataWrapper = {165, 226, 114, 216}
        Else
            SearchStart = {92, 217, 160, 35}
            'SearchStringStart = BitConverter.ToString(SearchStart)
            dataWrapper = {165, 226, 114, 216}
        End If


        'Console.WriteLine(SearchStringStart)
        Dim Startindex = 0
        Dim Endindex = 1
        Dim bFound As Boolean = True

        Dim fileCounter As Integer = 0
        Dim pos As Integer = 0
        Dim encoding As New System.Text.ASCIIEncoding

        Dim wavByte(), sampleByte() As Byte
        Dim wavSize, sampleRate, wavinbankint As Integer
        Dim header, newWavFile, bytedata, wavinbankbyte As Byte()
        Dim wavname As String
        Dim overallcounter = 0
        Dim savePosition(databankcount) As Integer

        'dataPosition for later, not needed for bik
        If wavtype <> "bik" Then
            savePosition = FindDataStart(sndfile, dataWrapper)
        End If


        'file functions
        If (Not System.IO.Directory.Exists(sndfile.Substring(0, sndfile.Length - 4))) Then
            My.Computer.FileSystem.CreateDirectory(sndfile.Substring(0, sndfile.Length - 4))
        End If
        Dim b() As Byte = IO.File.ReadAllBytes(sndfile) 'read file as array of bytes 
        Dim fs As New IO.FileStream(sndfile, IO.FileMode.Open)
        Dim binary_reader As New IO.BinaryReader(fs)

#End Region
        If wavtype = "bik" Then
#Region "PC Movie File"
            For i As Integer = 0 To b.Length - SearchStart.Length - 1
                If b(i) = SearchStart(0) Then
                    bFound = True
                    For j As Integer = 0 To SearchStart.Length - 1
                        If b(i + j) <> SearchStart(j) Then
                            bFound = False
                            Exit For
                        End If
                    Next
                    If bFound Then
                        fileCounter += 1 'start at 1, count up
                        'If fileCounter = 1 Then
                        'UpdateProjectLog("Skip 1...")
                        'Else
                        'Read size of wave, convert to Uint32
                        fs.Position = i + 4
                        wavByte = binary_reader.ReadBytes(4)
                        wavSize = BitConverter.ToUInt32(wavByte, 0)
                        wavSize += 8

                        'Read raw wav data
                        fs.Position = i
                        bytedata = binary_reader.ReadBytes(wavSize)

                        addlinetoprojectlog = wavtype & " video header found at byte: " & i & " Size: " & wavSize & vbCr
                        UpdateProjectLog(addlinetoprojectlog)

                        wavname = sndfile.Substring(0, sndfile.Length - 4) & "\movie" + fileCounter.ToString() + "." + wavtype

                        'write said file
                        My.Computer.FileSystem.WriteAllBytes(wavname, bytedata, False)

                        Startindex = i
                        'End If
                    End If
                End If
                'End If
            Next
#End Region
        Else
#Region "Sound Code (and pss)"
            For i As Integer = 0 To b.Length - SearchStart.Length - 1
                If b(i) = SearchStart(0) Then
                    bFound = True
                    For j As Integer = 0 To SearchStart.Length - 1
                        If b(i + j) <> SearchStart(j) Then
                            bFound = False
                            Exit For
                        End If
                    Next
                    If bFound Then
                        fileCounter += 1 'start at 1, count up
                        overallcounter += 1
                        If fileCounter = 1 And wavtype <> "pss" Then
                            'If overallcounter = 0 Then
                            'skip 1, its the loneliest number
                            'Actually, the first number in a bank is the overall size of the bank with the amount of wavs in it
                            fs.Position = i - 4
                            wavinbankbyte = binary_reader.ReadBytes(4) ' there are files with two banks.  darn coding
                            wavinbankint = BitConverter.ToUInt32(wavinbankbyte, 0)
                            UpdateProjectLog(wavinbankint & " " & wavtype & " in bank at " & i & vbCr)
                        Else
                            'Read samplerate, convert to uint32
                            If wavtype <> "pss" Then
                                fs.Position = i - 4
                                sampleByte = binary_reader.ReadBytes(4)
                                sampleRate = BitConverter.ToUInt32(sampleByte, 0)
                            End If

                            'Read size of wave, convert to Uint32
                            fs.Position = i + 4
                                wavByte = binary_reader.ReadBytes(4)
                                wavSize = BitConverter.ToUInt32(wavByte, 0)


                            'Read raw data
                            fs.Position = savePosition(pos)

                            If wavtype = "pss" And overallcounter > 1 Then
                                fs.Position += 2044 'buffer for pss files
                            ElseIf wavtype = "wavIPCM" And overallcounter > 1 Then 'not in bnk?  .lvl aligned by block size
                                Dim blocksize = 2048  'defined in req
                                'UpdateProjectLog(((savePosition(pos) + wavSize) Mod blocksize) & vbCr)
                                If ((savePosition(pos) + wavSize) Mod blocksize) <> 0 Then 'some files have 0, which confuses this function.
                                    wavSize += (blocksize - ((savePosition(pos) + wavSize) Mod blocksize))
                                End If
                            End If


                                UpdateProjectLog("Data start at byte " & fs.Position & " for file " & overallcounter & vbCr)

                            bytedata = binary_reader.ReadBytes(wavSize) 'PLEASE CHECK FOR ACCURACY
                            savePosition(pos) = fs.Position

                            addlinetoprojectlog = wavtype & " header found at byte: " & i & " Size: " & wavSize & " Sample Rate: " & sampleRate & vbCr
                            UpdateProjectLog(addlinetoprojectlog)

                                'file generator code
                                'PSS does not need a header
                                If wavtype = "pss" Then
                                    newWavFile = bytedata
                                wavname = sndfile.Substring(0, sndfile.Length - 4) & "\ps2movie" + overallcounter.ToString() + ".pss"
                            Else
                                header = AddHeader(wavtype, wavSize, sampleRate)
                                newWavFile = header.Concat(bytedata).ToArray()
                                    wavname = sndfile.Substring(0, sndfile.Length - 4) & "\rawwav" + overallcounter.ToString() + ".wav"
                                End If

                                'write said file
                                My.Computer.FileSystem.WriteAllBytes(wavname, newWavFile, False)

                                Startindex = i

                                If fileCounter - 1 = wavinbankint Then
                                    fileCounter = 0  'reset file counter for bank
                                    If databankcount > pos Then
                                        pos = pos + 1    'for our saveposition bank array
                                    End If
                                End If
                            End If
                        End If
                End If
            Next

            Dim newlength = Endindex - Startindex
            For i = Startindex To Endindex
                'currently just grabs teh position numbers, want character at that position number
                Dim ss As String = System.Text.Encoding.GetEncoding(1252).GetString(encoding.GetBytes(i))
                addlinetoprojectlog = ss & vbCr
                'newprojectlog = newprojectlog & vbCr & addlinetoprojectlog
                UpdateProjectLog(addlinetoprojectlog)
            Next
#End Region
        End If

        fs.Close()
        binary_reader.Close()
#End Region

    End Sub

End Module