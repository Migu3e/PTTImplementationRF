using Client.Classes;
using Client.Classes.AudioHandler;
using Client.Interfaces;

IClientStarter clientStarter = new ClientStarter(new FullAudioMaker(), new Receiver(), new Sender());
await clientStarter.StartAsync();