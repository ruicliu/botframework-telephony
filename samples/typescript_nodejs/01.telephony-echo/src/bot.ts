// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { ActivityHandler, MessageFactory } from "botbuilder";

export class EchoBot extends ActivityHandler {
  constructor() {
    super();

    const voiceModel = (text: string, voiceId: string, locale: string) => `
      <speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="${locale}">
        <voice name="${voiceId}">
          ${text}
        </voice>
      </speak>`;

    // See https://aka.ms/about-bot-activity-message to learn more about the message and other activity types.
    this.onMessage(async (context, next) => {
      const replyText = `Echo: ${context.activity.text}`;
      await context.sendActivity(
        MessageFactory.text(
          replyText,
          voiceModel(replyText, "en-US-AriaNeural", "en-US")
        )
      );
      // By calling next() you ensure that the next BotHandler is run.
      await next();
    });

    this.onMembersAdded(async (context, next) => {
      const membersAdded = context.activity.membersAdded;
      const welcomeText = "Hello and welcome!";
      for (const member of membersAdded) {
        if (member.id !== context.activity.recipient.id) {
          await context.sendActivity(
            MessageFactory.text(
              welcomeText,
              voiceModel(welcomeText, "en-US-AriaNeural", "en-US")
            )
          );
        }
      }
      // By calling next() you ensure that the next BotHandler is run.
      await next();
    });
  }
}
