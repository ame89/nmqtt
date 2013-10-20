/* 
 * nMQTT, a .Net MQTT v3 client implementation.
 * http://wiki.github.com/markallanson/nmqtt
 * 
 * Copyright (c) 2009 Mark Allanson (mark@markallanson.net) & Contributors
 *
 * Licensed under the MIT License. You may not use this file except 
 * in compliance with the License. You may obtain a copy of the License at
 *
 *     http://www.opensource.org/licenses/mit-license.php
*/

using System;

using Nmqtt;
using Moq;
using Xunit;

namespace NmqttTests.SubscriptionsManager
{
    public class SubscriptionsManagerTests
    {
        [Fact]
        public void Ctor()
        {
            var chMock = new Mock<IMqttConnectionHandler>();
            var pubMock = new Mock<IPublishingManager>();

            chMock.Setup(x => x.RegisterForMessage(MqttMessageType.SubscribeAck, It.IsAny<Func<MqttMessage, bool>>()));

            var sub = new Nmqtt.SubscriptionsManager(chMock.Object, pubMock.Object);
            
            chMock.VerifyAll();
            sub.Dispose();
        }

        [Fact]
        public void SubscriptionRequestCreatesPendingSubscription()
        {
            var chMock = new Mock<IMqttConnectionHandler>();
            var pubMock = new Mock<IPublishingManager>();

            const string topic = "testtopic";
            const MqttQos qos = MqttQos.AtMostOnce;

            // run and verify the mocks were called.
            var subs = new Nmqtt.SubscriptionsManager(chMock.Object, pubMock.Object);
            subs.RegisterSubscription<string, AsciiPayloadConverter>(topic, qos);

            Assert.Equal(SubscriptionStatus.Pending, subs.GetSubscriptionsStatus(topic));
        }

        [Fact]
        public void DisposeUnregistersMessageCallback()
        {
            var chMock = new Mock<IMqttConnectionHandler>();
            var pubMock = new Mock<IPublishingManager>();

            chMock.Setup(x => x.RegisterForMessage(MqttMessageType.SubscribeAck, It.IsAny<Func<MqttMessage, bool>>()));
            chMock.Setup(x => x.UnRegisterForMessage(MqttMessageType.SubscribeAck, It.IsAny<Func<MqttMessage, bool>>()));

            var subMgr = new Nmqtt.SubscriptionsManager(chMock.Object, pubMock.Object);
            subMgr.Dispose();

            chMock.VerifyAll();
        }

        [Fact]
        public void SubscriptionRequestInvokesSend()
        {
            MqttSubscribeMessage subMsg = null;
            var pubMock = new Mock<IPublishingManager>();

            var chMock = new Mock<IMqttConnectionHandler>();
            // mock the call to register and save the callback for later.
            chMock.Setup(x => x.RegisterForMessage(MqttMessageType.SubscribeAck, It.IsAny<Func<MqttMessage, bool>>()));
            // mock the call to Send(), which should occur when the subscription manager tries to subscribe
            chMock.Setup(x => x.SendMessage(It.IsAny<MqttSubscribeMessage>()))
                .Callback((MqttMessage msg) => subMsg = (MqttSubscribeMessage)msg);

            const string topic = "testtopic";
            const MqttQos qos = MqttQos.AtMostOnce;

            // run and verify the mocks were called.
            var subs = new Nmqtt.SubscriptionsManager(chMock.Object, pubMock.Object);
            subs.RegisterSubscription<string, AsciiPayloadConverter>(topic, qos);
            chMock.VerifyAll();

            // now check the message generated by the subscription manager was good - ie contain the topic at the specified qos
            Assert.Contains(topic, subMsg.Payload.Subscriptions.Keys);
            Assert.Equal(MqttQos.AtMostOnce, subMsg.Payload.Subscriptions[topic]);
        }

        [Fact]
        public void AcknowledgedSubscriptionRequestCreatesActiveSubscription()
        {
            Func<MqttMessage, bool> theCallback = null;
            MqttSubscribeMessage subMsg = null;
            var pubMock = new Mock<IPublishingManager>();

            var chMock = new Mock<IMqttConnectionHandler>();
            // mock the call to register and save the callback for later.
            chMock.Setup(x => x.RegisterForMessage(MqttMessageType.SubscribeAck, It.IsAny<Func<MqttMessage, bool>>()))
                .Callback((MqttMessageType msgtype, Func<MqttMessage, bool> cb) => theCallback = cb);
            // mock the call to Send(), which should occur when the subscription manager tries to subscribe
            chMock.Setup(x => x.SendMessage(It.IsAny<MqttSubscribeMessage>()))
                .Callback((MqttMessage msg) => subMsg = (MqttSubscribeMessage)msg);

            const string topic = "testtopic";
            const MqttQos qos = MqttQos.AtMostOnce;

            // run and verify the mocks were called.
            var subs = new Nmqtt.SubscriptionsManager(chMock.Object, pubMock.Object);
            subs.RegisterSubscription<string, AsciiPayloadConverter>(topic, qos);
            chMock.VerifyAll();

            // now check the message generated by the subscription manager was good - ie contain the topic at the specified qos
            Assert.Contains(topic, subMsg.Payload.Subscriptions.Keys);
            Assert.Equal(MqttQos.AtMostOnce, subMsg.Payload.Subscriptions[topic]);

            // execute the callback that would normally be initiated by the connection handler when a sub ack message arrived.
            theCallback(new MqttSubscribeAckMessage().WithMessageIdentifier(1).AddQosGrant(MqttQos.AtMostOnce));

            Assert.Equal(SubscriptionStatus.Active, subs.GetSubscriptionsStatus(topic));
        }
        
        [Fact]
        public void SubscriptionAckForNonPendingSubscriptionThrowsException()
        {
            Func<MqttMessage, bool> theCallback = null;
            MqttSubscribeMessage subMsg = null;
            var pubMock = new Mock<IPublishingManager>();

            var chMock = new Mock<IMqttConnectionHandler>();
            // mock the call to register and save the callback for later.
            chMock.Setup(x => x.RegisterForMessage(MqttMessageType.SubscribeAck, It.IsAny<Func<MqttMessage, bool>>()))
                .Callback((MqttMessageType msgtype, Func<MqttMessage, bool> cb) => theCallback = cb);
            // mock the call to Send(), which should occur when the subscription manager tries to subscribe
            chMock.Setup(x => x.SendMessage(It.IsAny<MqttSubscribeMessage>()))
                .Callback((MqttMessage msg) => subMsg = (MqttSubscribeMessage)msg);

            const string topic = "testtopic";
            const MqttQos qos = MqttQos.AtMostOnce;

            // run and verify the mocks were called.
            var subs = new Nmqtt.SubscriptionsManager(chMock.Object, pubMock.Object);
            subs.RegisterSubscription<string, AsciiPayloadConverter>(topic, qos);
            chMock.VerifyAll();

            // now check the message generated by the subscription manager was good - ie contain the topic at the specified qos
            Assert.Contains(topic, subMsg.Payload.Subscriptions.Keys);
            Assert.Equal(MqttQos.AtMostOnce, subMsg.Payload.Subscriptions[topic]);

            // execute the callback with a bogus message identifier.
            Assert.Throws<ArgumentException>(() => theCallback(new MqttSubscribeAckMessage().WithMessageIdentifier(999).AddQosGrant(MqttQos.AtMostOnce)));
        }

        [Fact]
        public void GetSubscriptionWithValidTopicReturnsSubscription()
        {
            Func<MqttMessage, bool> theCallback = null;
            var chMock = new Mock<IMqttConnectionHandler>();
            chMock.Setup(x => x.RegisterForMessage(MqttMessageType.SubscribeAck, It.IsAny<Func<MqttMessage, bool>>()))
                .Callback((MqttMessageType msgtype, Func<MqttMessage, bool> cb) => theCallback = cb);
            var pubMock = new Mock<IPublishingManager>();

            const string topic = "testtopic";
            const MqttQos qos = MqttQos.AtMostOnce;

            // run and verify the mocks were called.
            var subs = new Nmqtt.SubscriptionsManager(chMock.Object, pubMock.Object);
            subs.RegisterSubscription<string, AsciiPayloadConverter>(topic, qos);

            // execute the callback that would normally be initiated by the connection handler when a sub ack message arrived.
            theCallback(new MqttSubscribeAckMessage().WithMessageIdentifier(1).AddQosGrant(MqttQos.AtMostOnce));

            Assert.NotNull(subs.GetSubscription(topic));
        }

        [Fact]
        public void GetSubscriptionWithInvalidTopicReturnsNull()
        {
            Func<MqttMessage, bool> theCallback = null;
            var pubMock = new Mock<IPublishingManager>();

            var chMock = new Mock<IMqttConnectionHandler>();
            chMock.Setup(x => x.RegisterForMessage(MqttMessageType.SubscribeAck, It.IsAny<Func<MqttMessage, bool>>()))
                .Callback((MqttMessageType msgtype, Func<MqttMessage, bool> cb) => theCallback = cb);

            const string topic = "testtopic";
            const MqttQos qos = MqttQos.AtMostOnce;

            // run and verify the mocks were called.
            var subs = new Nmqtt.SubscriptionsManager(chMock.Object, pubMock.Object);
            subs.RegisterSubscription<string, AsciiPayloadConverter>(topic, qos);

            // execute the callback that would normally be initiated by the connection handler when a sub ack message arrived.
            theCallback(new MqttSubscribeAckMessage().WithMessageIdentifier(1).AddQosGrant(MqttQos.AtMostOnce));

            Assert.Null(subs.GetSubscription("abc_badTopic"));
        }

        [Fact]
        public void GetSubscriptionForPendingSubscriptionReturnsNull() {
            var chMock = new Mock<IMqttConnectionHandler>();
            var pubMock = new Mock<IPublishingManager>();

            const string topic = "testtopic";
            const MqttQos qos = MqttQos.AtMostOnce;

            // run and verify the mocks were called.
            var subs = new Nmqtt.SubscriptionsManager(chMock.Object, pubMock.Object);
            subs.RegisterSubscription<string, AsciiPayloadConverter>(topic, qos);

            Assert.Null(subs.GetSubscription(topic));
        }
    }
}
