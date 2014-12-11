Imports System.IO
Imports System.Net.Sockets
Imports System.Net

Module Server
    Dim starttime As Date
    Dim onlinenumber As Integer
    Dim serverversion As String = "v0.01 - Alpha"
    Private server As TcpListener
    Private client As New TcpClient
    Private ipendpoint As IPEndPoint = New IPEndPoint(IPAddress.Any, 2333)
    Private list As New List(Of Connection)

    Private Structure Connection
        Dim stream As NetworkStream
        Dim streamw As StreamWriter
        Dim streamr As StreamReader
        Dim nick As String
        Dim status As Boolean
        Dim room As String
    End Structure

    Private Sub SendToAllClients(ByVal s As String)
        For Each c As Connection In list
            Try
                c.streamw.WriteLine(s)
                c.streamw.Flush()
            Catch
            End Try
        Next
    End Sub

    Sub Main()
        starttime = System.DateTime.Now
        ServerLog("FuzzySRV " & serverversion & " (c) Peter Kowalsky")
        ServerLog("Server is starting!")
        onlinenumber = 0
        server = New TcpListener(ipendpoint)
        ServerLog("Starting the Listener on port " + ipendpoint.Port.ToString + "!")
        server.Start()
        ServerLog("Server started in " + CInt(System.DateTime.Now.Subtract(starttime).TotalMilliseconds).ToString + " ms.")
        While True
            client = server.AcceptTcpClient

            Dim c As New Connection
            ClientLog("New Client is connecting!")
            c.stream = client.GetStream
            c.streamr = New StreamReader(c.stream)
            c.streamw = New StreamWriter(c.stream)
            onlinenumber = onlinenumber + 1
            list.Add(c)

            Dim t As New Threading.Thread(AddressOf ListenToConnection)
            t.Start(c)
        End While
    End Sub

    Sub ServerLog(ByVal s As String)
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine(System.DateTime.Now.ToString + " [SERVER] " + s)
    End Sub

    Sub ClientLog(ByVal s As String)
        Console.ForegroundColor = ConsoleColor.Yellow
        Console.WriteLine(System.DateTime.Now.ToString + " [CLIENT] " + s)
    End Sub

    Private Sub ListenToConnection(ByVal con As Connection)
        Do
            Try
                Dim streamrawin As String = con.streamr.ReadLine
                Dim Input() As String
                'Hier jetzt wichtig String spliten, Splitzeichen ist "\"
                'Wenn ein unbekannter Befehl kommt, ignorieren.
                'Eigendlich performanter mit Packets, aber so geht es auch. Habe ja kein Monat dafür Zeit :3
                Input = streamrawin.Split("§")

                Select Case Input(1)

                    Case "sc_handshake"
                        'if Abfrage zum User bzw IP einbauen!
                        con.status = 1
                        con.streamw.WriteLine("TTT§sc_handshake§Test" & con.status)
                        con.streamw.Flush()
                        If con.status = 0 Then
                            list.Remove(con)
                            ClientLog("Handshake failed.")
                        Else
                            ClientLog("Handshake finished.")
                        End If

                    Case "login_request"
                        'if Abfrage zum schauen ob User existiert bzw neu anlegen, Format in Datei = User;MD5 Hash - Vl für File Zeugs eigenes Modul nehmen.
                        ' con.nick den nick eintragen - con.status setzen auf false wenn PW falsch, sonst true beibehalten.
                        con.streamw.WriteLine("login_result" & "\" & con.status)
                        con.streamw.Flush()
                        con.streamw.WriteLine("room_list" & "\" & "lobby, help, games, dinner, afk")
                        con.streamw.Flush()
                        If con.status = 0 Then
                            list.Remove(con)
                            ClientLog("Login from " & con.nick & " failed.")
                        Else
                            ClientLog("Login from " & con.nick & " finished.")
                            SendToAllClients("login_user\" & con.nick)
                            con.room = "lobby"
                        End If

                    Case "chat_request"
                        'Vielleicht Spamfilter einbauen, manche Schlagwörter filten und erstetzen, bzw User kicken.
                        'Chat an alle Clients weiter senden.
                        If con.room = "afk" Then
                            con.streamw.WriteLine("chat_event\" & "You are in the AFK Channel. You can´t write." & "\" & con.nick & "\" & con.room)
                        Else
                            SendToAllClients("chat_event\" & "chat...." & "\" & con.nick & "\" & con.room)
                        End If

                    Case "get_infos"
                        'Willste Info? Bekommste.
                        con.streamw.WriteLine("send_infos\" & onlinenumber & "\" & serverversion)
                        con.streamw.Flush()

                    Case "change_room"
                        con.room = "lobby"

                    Case "my_room"
                        con.streamw.WriteLine("current_room\" & con.room)
                        con.streamw.Flush()

                End Select


            Catch
                list.Remove(con)
                SendToAllClients("disconnect" & "\" & con.nick)
                Exit Do
            End Try
        Loop
    End Sub


End Module
