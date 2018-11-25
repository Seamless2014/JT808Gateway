﻿using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using JT808.DotNetty.Codecs;
using JT808.DotNetty.Handlers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JT808.DotNetty
{
    public class JT808SimpleTcpClient
    {
        private Bootstrap cb;

        private MultithreadEventLoopGroup clientGroup;

        private IChannel clientChannel;

        public JT808SimpleTcpClient(EndPoint remoteAddress)
        {
            clientGroup = new MultithreadEventLoopGroup(1);
            cb = new Bootstrap()
                .Group(clientGroup)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<TcpSocketChannel>(channel =>
                {
                    channel.Pipeline.AddLast("jt808Buffer", new DelimiterBasedFrameDecoder(int.MaxValue,
                        Unpooled.CopiedBuffer(new byte[] { JT808.Protocol.JT808Package.BeginFlag }),
                        Unpooled.CopiedBuffer(new byte[] { JT808.Protocol.JT808Package.EndFlag })));
                    channel.Pipeline.AddLast("jt808Decode", new JT808ClientDecoder());
                }));
            clientChannel = cb.ConnectAsync(remoteAddress).Result;
        }

        public void WriteAsync(byte[] data)
        {
            clientChannel.WriteAndFlushAsync(Unpooled.WrappedBuffer(data));
        }

        public void Down()
        {
            this.clientChannel?.CloseAsync().Wait();
            Task.WaitAll(this.clientGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
        }
    }
}
