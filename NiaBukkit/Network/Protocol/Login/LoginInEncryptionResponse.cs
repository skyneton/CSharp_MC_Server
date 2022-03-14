using NiaBukkit.API.Util;

namespace NiaBukkit.Network.Protocol.Login
{
	public class LoginInEncryptionResponse
	{
        internal static void Read(NetworkManager networkManager, ByteBuf buf)
        {
	        //TODO: Encrpypion
	        var sharedSecret = buf.Read(buf.ReadVarInt());
	        var verifyToken = buf.Read(buf.ReadVarInt());
	        
	        networkManager.EncryptionResponse(SelfCryptography.Decrypt(sharedSecret), SelfCryptography.Decrypt(verifyToken));
        }
	}
}