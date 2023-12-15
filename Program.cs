using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

class Program
{
    static string openaiApiKey = "yourGPTAPI"; // Replace with your OpenAI API key
    static string gptChatApiUrl = "https://api.openai.com/v1/chat/completions";
    static string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
    static string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");

    static async Task Main(string[] args)
    {
        Console.WriteLine("Listening for questions. Say or type your question, or press Enter to exit...");

        while (true)
        {
            string transcribedText;

            // Step 1: Speech-to-Text
            transcribedText = await PerformSpeechToText();

            // Step 2: Chat API (GPT)
            var gptApiResponse = await SendToGptChatApi(transcribedText);

            // Step 3: Extract relevant information from GPT API response
            var processedText = ExtractInformationFromGptResponse(gptApiResponse);

            // Step 4: Text-to-Speech
            await PerformTextToSpeech(processedText);
        }
    }

    async static Task<string> PerformSpeechToText()
    {
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechRecognitionLanguage = "en-US";

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

        Console.WriteLine("Speak into your microphone.");
        var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();

        var recognizedText = speechRecognitionResult.Text;
        Console.WriteLine($"Recognized Text: {recognizedText}");

        return recognizedText;
    }


    async static Task<string> SendToGptChatApi(string text)
    {
        using var httpClient = new HttpClient();
        var requestContent = new StringContent(
            $"{{\"model\": \"gpt-3.5-turbo\", \"messages\": [{{\"role\": \"user\", \"content\": \"{text}\"}}]}}",
            Encoding.UTF8,
            "application/json"
        );
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openaiApiKey}");

        var response = await httpClient.PostAsync(gptChatApiUrl, requestContent);
        return await response.Content.ReadAsStringAsync();
    }

    static string ExtractInformationFromGptResponse(string gptApiResponse)
    {
        // Parse the JSON response to extract the assistant's content
        var responseObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(gptApiResponse);
        var assistantContent = responseObj.choices[0].message.content;

        return assistantContent;
    }

    async static Task PerformTextToSpeech(string text)
    {
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechSynthesisVoiceName = "en-US-ElizabethNeural";

        using var audioConfig = AudioConfig.FromDefaultSpeakerOutput();
        using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

        var result = await synthesizer.SpeakTextAsync(text);
        OutputSpeechSynthesisResult(result, text);
    }

    static void OutputSpeechSynthesisResult(SpeechSynthesisResult result, string text)
    {
        switch (result.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                Console.WriteLine($"Speech synthesized for text: [{text}]");
                break;
            case ResultReason.Canceled:
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }
                break;
            default:
                break;
        }
    }
}
