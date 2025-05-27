# D-ID Avatar Setup Guide for AliveOnD-ID

## 🎯 Quick Start Checklist

1. ✅ **Get D-ID API credentials**
2. ✅ **Configure appsettings.json**
3. ✅ **Add the new controller and test page**
4. ✅ **Test avatar functionality**
5. ✅ **Integrate with your chat system**

---

## 1. Get D-ID API Credentials

### Sign up for D-ID Account
1. Go to [D-ID Studio](https://studio.d-id.com)
2. Create an account or sign in
3. Navigate to **API** section in dashboard
4. Copy your **API Key**

### Get Presenter and Driver IDs
1. In D-ID Studio, go to **Presenters** section
2. Choose or upload a presenter image
3. Copy the **Presenter ID** (e.g., `jack-Pt27VkP3hW`)
4. Go to **Drivers** section 
5. Choose a driver (voice/animation style)
6. Copy the **Driver ID** (e.g., `fbQicImV2J`)

---

## 2. Configure Your Application

### Update appsettings.json
Replace the D-ID section with your real credentials:

```json
{
  "Services": {
    "DID": {
      "ApiKey": "YOUR_ACTUAL_D-ID_API_KEY",
      "BaseUrl": "https://api.d-id.com",
      "PresenterId": "YOUR_PRESENTER_ID",
      "DriverId": "YOUR_DRIVER_ID"
    }
  }
}
```

### Environment Variables (Optional but Recommended)
For security, you can use environment variables:

**Windows PowerShell:**
```powershell
$env:D_ID_API_KEY = "your-actual-api-key"
```

Then in appsettings.json:
```json
"ApiKey": "D_ID_API_KEY"
```

---

## 3. Add New Files to Your Project

### Add the Avatar Test Controller
1. Create `Controllers/AvatarTestController.cs`
2. Copy the content from the "Avatar Test Controller" artifact above

### Add the Avatar Test Page
1. Create `Pages/AvatarTest.razor`
2. Copy the content from the "Avatar Test Page" artifact above

### Update Navigation (Optional)
Add to `Shared/NavMenu.razor`:
```html
<div class="nav-item px-3">
    <NavLink class="nav-link" href="avatar-test">
        <span class="oi oi-camera-slr" aria-hidden="true"></span> Avatar Test
    </NavLink>
</div>
```

---

## 4. Build and Test

### Build the Project
```powershell
dotnet build
dotnet run
```

### Test Via Swagger (API Testing)
1. Navigate to: `https://localhost:7031/swagger`
2. Look for `AvatarTest` endpoints
3. Test the workflow:
   - **POST** `/api/AvatarTest/create-stream` → Get TestSessionId
   - **POST** `/api/AvatarTest/speak/{testSessionId}` → Make avatar speak

### Test Via UI (User-Friendly)
1. Navigate to: `https://localhost:7031/avatar-test`
2. Click **"Create Stream"**
3. Enter text in the speech box
4. Click **"Speak"**

---

## 5. Testing Workflow

### Basic Avatar Speech Test

**Step 1: Create Stream**
```bash
curl -X POST "https://localhost:7031/api/AvatarTest/create-stream" \
  -H "Content-Type: application/json" \
  -d "{}"
```

**Step 2: Make Avatar Speak**
```bash
curl -X POST "https://localhost:7031/api/AvatarTest/speak/{testSessionId}" \
  -H "Content-Type: application/json" \
  -d "{
    \"text\": \"Hello! I am your AI avatar.\",
    \"sessionId\": \"your-session-id\",
    \"emotion\": \"happy\"
  }"
```

### Expected Results
✅ **Success:** Avatar stream created, text sent to D-ID API  
✅ **Audio:** Avatar should generate speech audio  
⚠️ **Video:** WebRTC video stream requires additional setup  

---

## 6. Understanding the Current Implementation

### What Works Now
- ✅ **Stream Creation:** Creates D-ID avatar streams
- ✅ **Text-to-Speech:** Sends text to avatar for speech generation
- ✅ **API Integration:** Full D-ID Clips API integration
- ✅ **Error Handling:** Proper logging and error responses

### What Needs WebRTC (Future Enhancement)
- 🔄 **Video Display:** Real-time video streaming to browser
- 🔄 **Full Interactivity:** Complete bidirectional communication

### Current Limitations
- **No Real Video:** The video element won't show the avatar yet (needs WebRTC)
- **Audio Only:** The avatar generates speech but video display requires WebRTC setup
- **Mock Connection:** The test UI shows "Connected (Mock)" status

---

## 7. Integration with Your Chat System

### Connect to ChatLayout Component
Once avatar speech testing works, you can integrate it with your chat:

```csharp
// In your ChatLayout.razor SendTextMessage method:
private async Task ProcessLLMResponse(string userMessage)
{
    // 1. Get LLM response
    var llmResponse = await LLMService.GetResponseAsync(userMessage);
    
    // 2. Send to avatar
    if (!string.IsNullOrEmpty(CurrentStreamId))
    {
        await AvatarService.SendTextToAvatarAsync(
            CurrentStreamId, 
            CurrentSessionId, 
            llmResponse.Text, 
            llmResponse.Emotion);
    }
}
```

---

## 8. Troubleshooting

### Common Issues

**"Stream creation failed"**
- ✅ Check API key is correct
- ✅ Verify presenter and driver IDs exist
- ✅ Check D-ID account has credits

**"Failed to send text to avatar"**
- ✅ Ensure stream was created successfully
- ✅ Check text is not empty
- ✅ Verify session ID matches

**"No video showing"**
- ⚠️ This is expected - WebRTC video setup needed
- ✅ Audio/speech generation should still work

### Debug Steps
1. Check Swagger endpoints work first
2. Test with simple text like "Hello"
3. Check browser console for errors
4. Verify D-ID account credits and limits

---

## 9. Next Steps

1. **✅ Get basic avatar speech working** (this guide)
2. **🔄 Add WebRTC video streaming** (future enhancement)
3. **🔄 Connect to LLM responses** (easy swap later)
4. **🔄 Add emotion detection from LLM** (enhancement)

---

## Ready to Test! 🚀

Follow the steps above, then test your avatar speech functionality. Once you can make the avatar "speak" text via the API/UI, you'll have the foundation ready for LLM integration!