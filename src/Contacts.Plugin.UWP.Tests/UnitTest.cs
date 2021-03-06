﻿using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Plugin.Contacts.UWP.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [ClassInitialize]
        public static void SetupTests(TestContext testContext)
        {
            contacts = new List<Contact>();
            contacts.Add(AddContact("sala", "sali", "0009292992").Result);
            contacts.Add(AddContact("asala", "tsali", "1009292992").Result);
            contacts.Add(AddContact("bsala", "ysali", "2009292992").Result);
        }

        [ClassCleanup]
        public static void CleanUpTests()
        {
            if (contacts != null)
                foreach (Contact contact in contacts)
                    DeleteContact(contact).Wait();
        }

        [TestMethod]
        public void GeneralTest()
        {
            var contactsImplementation = new ContactsImplementation();
            if (contactsImplementation.RequestPermission().Result)
            {
                List<Plugin.Contacts.Abstractions.Contact> contacts = null;
                contactsImplementation.PreferContactAggregation = false;

                Task.Run(() =>
                {
                    Assert.IsNotNull(contactsImplementation.Contacts);
                    contacts = contactsImplementation.Contacts
                        .Where(c => c.Phones.Count > 0 && c.FirstName.Length >= 5)
                        .ToList();

                    Assert.IsNotNull(contacts);
                    Assert.AreEqual(2, contacts.Count);
                });
            }
        }

        [TestMethod]
        public void ContactMapToPluginContactShouldBeWork()
        {
            var contact = new Contact()
            {
                FirstName = "lalal",
                LastName = "hghghg"
            };

            Plugin.Contacts.Abstractions.Contact pluginContact = UWPContactMapToPluginContact.Mapper.Map<Contact,
                Plugin.Contacts.Abstractions.Contact>(contact);

            Assert.IsNotNull(pluginContact);
            Assert.AreEqual(contact.FirstName, pluginContact.FirstName);
            Assert.AreEqual(contact.LastName, pluginContact.LastName);
        }

        [TestMethod]
        public void PhoneContactMapToPluginContactShouldBeWork()
        {
            var contact = new Contact()
            {
                FirstName = "lalal",
                LastName = "hghghg"
            };

            contact.Phones.Add(new ContactPhone()
            {
                Description = "Test number",
                Kind = ContactPhoneKind.Mobile,
                Number= "1234567890"
            });

            Plugin.Contacts.Abstractions.Contact pluginContact = UWPContactMapToPluginContact.Mapper.Map<Contact,
                Plugin.Contacts.Abstractions.Contact>(contact);

            Assert.IsNotNull(pluginContact);
            Assert.AreEqual(contact.FirstName, pluginContact.FirstName);
            Assert.AreEqual(contact.LastName, pluginContact.LastName);
            Assert.AreEqual(contact.Phones.First().Number, pluginContact.Phones.First().Number);
        }

        [TestMethod]
        public void CollectionContactsMapToPluginContactsShouldBeWoek()
        {
            IReadOnlyList<Contact> contacts = new List<Contact>()
            {
                CreateContact("ttt", "qqqq"),
                CreateContact("ggg", "aaa"),
                CreateContact("bbb", "zzz"),
            };

            IEnumerable<Plugin.Contacts.Abstractions.Contact> pluginContacts =
                UWPContactMapToPluginContact.Mapper.Map<IReadOnlyList<Contact>,
                IEnumerable<Plugin.Contacts.Abstractions.Contact>>(contacts);
        }

        private Contact CreateContact(string firstName, string lastName)
        {
            var contact = new Contact()
            {
                FirstName = firstName,
                LastName = lastName
            };

            var random = new Random();
            contact.Phones.Add(new ContactPhone()
            {
                Description = firstName + " " + lastName + " phone",
                Kind = ContactPhoneKind.Mobile,
                Number = random.Next((int)Math.Pow(10, 5), (int)Math.Pow(10, 6) - 1).ToString()
            });

            return contact;
        }


        private static async Task<Contact> AddContact(string firstName, string lastName, string phoneNumber)
        {
            // Thanks to http://stackoverflow.com/a/34652963/1539100

            var contact = new Contact()
            {
                FirstName = firstName,
                LastName = lastName
            };

            contact.Phones.Add(new ContactPhone()
            {
                Number = phoneNumber,
                Kind = ContactPhoneKind.Mobile
            });

            /*
             * Get he contact store for the app (so no lists from outlook
             * and other stuff will be in the returned lists..)
             */
            var contactStore = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);

            IReadOnlyList<ContactList> contactLists = await contactStore.FindContactListsAsync();

            ContactList contactList = contactLists.Where(x => x.DisplayName == "TestList").FirstOrDefault()
                ?? await contactStore.CreateContactListAsync("TestList");

            await contactList.SaveContactAsync(contact);

            return contact;
        }

        //you can obviusly couple the changes better then this... this is just to show the basics 
        private static async Task DeleteContact(Contact contactToRemove)
        {
            var contactStore = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);

            ContactList contactList = await contactStore.GetContactListAsync(contactToRemove.ContactListId);

            Contact contact = await contactList.GetContactAsync(contactToRemove.Id);

            await contactList.DeleteContactAsync(contact);
        }

        private static IList<Contact> contacts;
    }
}
