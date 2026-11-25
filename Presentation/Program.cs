using Application;
using Application.Interfaces;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Presentation;
using Presentation.Clients;
using Presentation.Configs;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;

services.Configure<FChatConfig>(builder.Configuration.GetSection("FChat"));

services.AddHttpClient();
services.AddSingleton<IDataPathProvider, DataPathProvider>();
services.AddSingleton<IFileOperations, FileRepository>();
services.AddSingleton<IChatTranscriptLogger, ChatTranscriptLogger>();
services.AddSingleton<IGloryholeProfileRepository, GloryholeProfileRepository>();
services.AddSingleton<IGloryholeManager>(sp =>
    new GloryholeManager(sp.GetRequiredService<IOptions<FChatConfig>>().Value.AutoJoinChannelCode));
services.AddSingleton<IRoomProvider>(sp =>
    new RoomProvider(sp.GetRequiredService<IOptions<FChatConfig>>().Value.AutoJoinChannelCode));
services.AddSingleton<IModeratorRepository, ModeratorRepository>();
services.AddSingleton<IBannedUserRepository, BannedUserRepository>();
services.AddSingleton<IBotStatusRepository, BotStatusRepository>();
services.AddSingleton<IBlowbucksRepository, BlowbucksRepository>();
services.AddSingleton<IBoothItemCatalog, BoothItemCatalog>();
services.AddSingleton<IOwnedBoothItemsRepository, OwnedBoothItemsRepository>();
services.AddScoped<IFChatService, FChatService>();
services.AddSingleton<IFChatClient, FChatClient>();
services.AddSingleton<IModuleDispatcher, ModuleDispatcher>();
services.AddSingleton<IInboundMessageQueue, InboundMessageQueue>();
services.AddSingleton<IOutboundMessageQueue, OutboundMessageQueue>();
services.AddSingleton<ICharacterPresenceTracker, CharacterPresenceTracker>();
services.AddModulesFromAssembly(typeof(ModuleBase).Assembly);

services.AddHostedService<FChatWorker>();
services.AddHostedService<DispatcherWorker>();
services.AddHostedService<OutboundWorker>();
services.AddHostedService<GloryholeWatcherWorker>();

var app = builder.Build();
app.Run();
