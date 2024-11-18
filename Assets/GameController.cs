using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Specialized;
using System.Net.Security;
using System.Numerics;
using System.Net.Http.Headers;
using System.ComponentModel.Design;
using System.Xml.Serialization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using static UnityEditor.Experimental.GraphView.GraphView;
using TMPro;


// git test
class GameController : MonoBehaviour
{
    public static UnityEngine.Vector3 deckPosition = new UnityEngine.Vector3(0,2,0);
    public static GameController Instance { get; private set; }
    public Transform deckParent;
    public Transform GamesTable;
    public TMP_Text messageText;
    private Deck deck;
    private List<Player> players;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("More than one GameController in the scene!");
            Destroy(gameObject);
        }
    }
    void Start()
    {
        Player player1 = new Player(false, 1); // User
        Player player2 = new Player(true, 2); // CPUa
        deck = new Deck(1, deckParent, GamesTable, true);

        players = new List<Player> { player1, player2 };

        StartCoroutine(GameLoop());
        IEnumerator GameLoop()
        {
            while (deck.GetBackendDeck().Count >= 6)
            {
                foreach (Player player in players)
                {
                    CardSelection cards = new CardSelection(deck, player);
                    yield return StartCoroutine(cards.PromptPick(messageText)); // Wait for user to pick a card
                }
            }
            messageText.text = "You ran out of cards!";
            yield return new WaitForSeconds(2);

            List<int> points = new List<int>();
            players = players.OrderByDescending(player => player.points).ToList(); // Orders players from least to greatest based on points
            messageText.text = $"{players[0].name} won with {players[0].points} points!"; // players[0] is the winner

            foreach (Player player in players) { player.Reset(); } //Resets points after game is finished    
        }
    }

    class Player
    {
        public int points;
        public string name;
        public int id;
        public bool cpu;
        public Player(bool cpu, int id)
        {
            this.name = "Player" + id;
            this.id = id;
            this.cpu = cpu;
        }
        public void Reset() { points = 0; }
    }
    class Card
    {
        public string suit;
        public string name;
        public string rank;
        public int value;
        public int suitLevel;
        public int totalCardValue;
        /* totalCardValue:
        To compare the value of each card I combined the
        value of the most important value (1-10, Jack(11) ..etc) 
        and the suits that take precedence, which is Diamonds is 4, Hearts is 3, etc.

        For example: Together that makes the lowest possible TCV (totalcardValue): 21 or 2 of clubs
                     The highest possible value is 144 or Ace of Diamonds, since an Ace is 14

        */


        public bool chosen = false;

        public Card()
        {
            suit = string.Empty;
            value = 0;
            
        }
        public Card(string card)
        {
            assignValues(card);
        }
        public void assignValues(string card)
        {
            var cardValues = card.Split(new string[] { " of " }, StringSplitOptions.None); // Splits  these into seperate variables
            string Value = cardValues[0].Trim();
            if (Value == "Jack") this.value = 11;

            else if (Value == "Queen") this.value = 12;

            else if (Value == "King") this.value = 13;

            else if (Value == "Ace") this.value = 14;

            else this.value = int.Parse(Value);

            this.rank = Value;

            this.suit = cardValues[1].Trim();

            this.suitLevel = Array.IndexOf(Deck.suits, this.suit) + 1;

            this.totalCardValue = value * 10 + suitLevel;

            this.name = $"{card}s";


        }


    }

    class Deck
    {
        private List<GameObject> visualDeck = new List<GameObject>();
        private List<Card> backendDeck = new List<Card>();
        public static string[] suits = { "Diamond", "Heart", "Spade", "Club" };
        public static string[] values = { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };
        public Transform deckParent;
        private Transform GamesTable;
        public TMP_Text messageText;

        public Deck(int copies, Transform deckParent, Transform GamesTable, bool shuffle)
        {
            this.GamesTable = GamesTable;
            this.deckParent = deckParent;
            for (int i = 0; i < copies; i++)
            {
                foreach (string value in values)
                {
                    foreach (string suit in suits) // Fills the deck using the arrays above
                    {
                        Card card1 = new Card($"{value} of {suit}");
                        backendDeck.Add(card1);
                    }
                }
            } // if (shuffle) Shuffle();
            GameController.Instance.StartCoroutine(SpawnCard(deckParent));
        }

        public IEnumerator SpawnCard(Transform deckParent)
        {
            List<Card> localDeck = new List<Card>(backendDeck);
            foreach (Card card in localDeck)
                {
                    string cardName = $"Card_{card.suit}{card.rank}";
                    GameObject cardPrefab = Resources.Load<GameObject>($"Prefabs/Individual Pieces/Cards/{card.suit}s/{cardName}");
                    if (cardPrefab != null)
                    {

                        // Spawn the card and apply rotation
                        GameObject visualCard = Instantiate(cardPrefab, deckPosition, UnityEngine.Quaternion.Euler(90, 0, 0), GamesTable);
                        Rigidbody rb = visualCard.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        }

                        visualCard.name = cardName;  // Set proper name 
                        visualCard.transform.localPosition = deckPosition;  // Set relative position to GamesTable

                        BoxCollider boxCollider = visualCard.GetComponent<BoxCollider>();
                        if (boxCollider != null)
                        {
                            UnityEngine.Vector3 newSize = boxCollider.size;
                            newSize.z = 0.002549444f;  // Set Z to your desired thickness
                            boxCollider.size = newSize;
                        }
                        else
                        {
                            messageText.text = $"{visualCard.name} is missing a box collider";
                        }

                        visualDeck.Add(visualCard);
                    yield return new WaitForSeconds(0.1f);
                    }
                    else
                    {
                        Debug.LogError($"Card prefab {cardName} not found!"); 
                    }
                }
        }
    public List<Card> GetBackendDeck() => backendDeck;

        public void Shuffle()
        {
            System.Random random = new System.Random();

            for (int i = backendDeck.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                Card temp = backendDeck[i];
                backendDeck[i] = backendDeck[j];
                backendDeck[j] = temp;
            }

            Debug.Log("Deck shuffled!");
        }
    }
    

    class CardSelection
    {
        private List<Card> cards = new List<Card>();
        private Card chosenCard;
        private Player player;
        private Deck deckObj;
        private List<Card> deck;

        public CardSelection(Deck deckObj, Player player)
        {
            this.deckObj = deckObj;
            this.deck = deckObj.GetBackendDeck();
            this.player = player;

            for (int i = 0; i < 3; i++)
            {
                var card = deck[0];
                cards.Add(card);
                deck.RemoveAt(0);
                
            }
        }

        public IEnumerator PromptPick(TMP_Text messageText)
        {
            // Display cards and ask player to pick
            messageText.text = $"{player.name}, pick a card: 1-3";
            System.Random random = new System.Random();
            int choice = player.cpu ? random.Next(1, 4) : -1;

            if (!player.cpu)
            {
                // Wait for player input (hook into Unity's button system)
                while (choice == -1)
                {
                    yield return null; // Wait for input
                }
            }

            Card chosenCard = cards[choice - 1];
            EvaluatePick(chosenCard, messageText);
        }
        private void EvaluatePick(Card chosenCard, TMP_Text messageText)
        {
            int value = cards.OrderByDescending(c => c.totalCardValue).ToList().IndexOf(chosenCard);

            switch (value)
            {
                case 0:
                    player.points += 10;
                    messageText.text = $"{player.name} got the highest! (+10)";
                    break;
                case 1:
                    player.points += 3;
                    messageText.text = $"{player.name} got the middle! (+3)";
                    break;
                case 2:
                    player.points -= 3;
                    messageText.text = $"{player.name} got the lowest.. (-3)";
                    break;
            }

            messageText.text += $"\n{player.name} now has {player.points} points!";
        }
    }
}

    